module AgentSystem
#nowarn "40"

[<AutoOpenAttribute>]
module LAgent =
    open Microsoft.FSharp.Control
    open System.Collections.Concurrent
    open System
        
    type AfterError<'state> =
    | ContinueProcessing of 'state
    | StopProcessing
    | RestartProcessing
        
    type MailboxProcessor<'a> with

        static member public SpawnAgent<'b>(messageHandler :'a->'b->'b, initialState : 'b, ?timeout:'b -> int,
                                            ?timeoutHandler:'b -> AfterError<'b>, ?errorHandler:Exception -> 'a option -> 'b -> AfterError<'b>) : MailboxProcessor<'a> =
            let timeout = defaultArg timeout (fun _ -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun state -> ContinueProcessing(state))
            let errorHandler = defaultArg errorHandler (fun _ _ state -> ContinueProcessing(state))
            MailboxProcessor.Start(fun inbox ->
                let rec loop(state) = async {
                    let! msg = inbox.TryReceive(timeout(state))
                    try
                        match msg with
                        | None      -> match timeoutHandler state with
                                        | ContinueProcessing(newState)    -> return! loop(newState)
                                        | StopProcessing        -> return ()
                                        | RestartProcessing     -> return! loop(initialState)
                        | Some(m)   -> return! loop(messageHandler m state)
                    with
                    | ex -> match errorHandler ex msg state with
                            | ContinueProcessing(newState)    -> return! loop(newState)
                            | StopProcessing        -> return ()
                            | RestartProcessing     -> return! loop(initialState)
                    }
                loop(initialState))

        static member public SpawnWorker(messageHandler,  ?timeout, ?timeoutHandler,?errorHandler) =
            let timeout = defaultArg timeout (fun () -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun _ -> ContinueProcessing(()))
            let errorHandler = defaultArg errorHandler (fun _ _ -> ContinueProcessing(()))
            MailboxProcessor.SpawnAgent((fun msg _ -> messageHandler msg; ()), (), timeout, timeoutHandler, (fun ex msg _ -> errorHandler ex msg))

        static member public SpawnParallelWorker(messageHandler, howMany, ?timeout, ?timeoutHandler,?errorHandler) =
            let timeout = defaultArg timeout (fun () -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun _ -> ContinueProcessing(()))
            let errorHandler = defaultArg errorHandler (fun _ _ -> ContinueProcessing(()))
            MailboxProcessor<'a>.SpawnAgent((fun msg (workers:MailboxProcessor<'a> array, index) ->
                                                workers.[index].Post msg
                                                (workers, (index + 1) % howMany))  
                                            , (Array.init howMany (fun _ -> MailboxProcessor<'a>.SpawnWorker(messageHandler, timeout, timeoutHandler, errorHandler)), 0))

    type Control<'msg, 'state> =
    | Restart
    | Stop
    | SetManager of AsyncAgent<AsyncAgent<'msg, 'state> * string * Exception * 'msg * 'state * 'state, unit>
    | SetName of string
    | SetAgentHandler of ('msg -> 'state -> 'state)
    | SetWorkerHandler of ('msg -> unit)
    | SetTimeoutHandler of int * ('state -> AfterError<'state>)
    and UC<'msg, 'state> =
    | User of 'msg
    | Control of Control<'msg, 'state>
    and AsyncAgent<'msg,'state>(messageHandler:'msg -> 'state -> 'state) =
        let mutable messageHandler = messageHandler
        let mutable timeout =  -1
        let mutable timeoutHandler = fun (state:'state) -> ContinueProcessing(state)
        let mutable mailbox:MailboxProcessor<UC<'msg, 'state>> = Unchecked.defaultof<MailboxProcessor<UC<'msg,'state>>>
        let mutable currentState:'state = Unchecked.defaultof<'state>
        let mutable manager = Unchecked.defaultof<AsyncAgent<AsyncAgent<'msg,'state> * string * Exception * 'msg * 'state * 'state, unit>>
        let mutable name = "Undefined"
        let mutable index = 0
        member a.Post msg = mailbox.Post (User(msg))
        member a.PostControl msg = mailbox.Post (Control(msg))
        member internal a.Index with get() = index and set(i) = index <- i
        member internal a.InitializeMailbox(mbox) = mailbox <- mbox
        member internal a.SetCurrentState(state) = currentState <- state
        member internal a.MessageHandler with get() = messageHandler and set(f) = messageHandler <- f
        member internal a.Timeout with get() = timeout and set(i) = timeout <- i
        member internal a.TimeoutHandler with get(): 'state -> AfterError<'state> = timeoutHandler and set(f) = timeoutHandler <- f
        member internal a.Manager with get() = manager and set(m) = manager <- m
        member internal a.Mailbox with get() = mailbox and set(m) = mailbox <- m
        member internal a.Name with get() = name and set(n) = name <- n

    exception ControlException
    
    let printDefaultError name ex msg state initialState =
            eprintfn "The exception below occurred on agent %s at state %A with message %A. The agent was started with state %A.\n%A" name state msg initialState ex
    
    let rec createAgentMailbox (agent:AsyncAgent<'msg, 'state>) initialState =
        MailboxProcessor<UC<'msg, 'state>>.SpawnAgent(
                            (fun msg state ->
                                agent.SetCurrentState(state)
                                match msg with
                                | Control(c) ->
                                    match c with
                                    | SetManager(m) -> agent.Manager <- m; state
                                    | SetName(s) -> agent.Name <- s; state
                                    | SetWorkerHandler(f) ->
                                            agent.MessageHandler <- (fun m _ -> f m; Unchecked.defaultof<'state>)
                                            state
                                    | SetAgentHandler(f) ->
                                            agent.MessageHandler <- f
                                            state
                                    | SetTimeoutHandler(tout, handler) ->
                                            agent.Timeout <- tout
                                            agent.TimeoutHandler <- handler; state
                                    | _ -> raise(ControlException)
                                | User(uMsg) ->
                                    agent.MessageHandler uMsg state),
                            initialState,
                            timeout = (fun _ -> agent.Timeout),
                            timeoutHandler = (fun state -> agent.TimeoutHandler state),
                            errorHandler = fun ex msg state ->
                                if msg.IsNone then
                                    if agent.Manager <> Unchecked.defaultof<AsyncAgent<AsyncAgent<'msg,'state> * string * Exception * 'msg * 'state * 'state, unit>> then
                                        agent.Manager.Post (agent, agent.Name, ex, Unchecked.defaultof<'msg>, state, initialState)
                                        ContinueProcessing(state)
                                    else
                                        printDefaultError agent.Name ex Unchecked.defaultof<'msg> state initialState
                                        ContinueProcessing(state) 
                                else
                                    let m = msg.Value
                                    match(m) with
                                    | Control(c) ->
                                        match(c) with
                                            | Restart   -> RestartProcessing
                                            | Stop      -> StopProcessing
                                            | _         -> ContinueProcessing(state)
                                    | User(msg) ->
                                        if agent.Manager <> Unchecked.defaultof<AsyncAgent<AsyncAgent<'msg,'state> * string * Exception * 'msg * 'state * 'state, unit>> then
                                            agent.Manager.Post (agent, agent.Name, ex, Unchecked.defaultof<'msg>, state, initialState)
                                            ContinueProcessing(state)
                                        else
                                            printDefaultError agent.Name ex Unchecked.defaultof<'msg> state initialState
                                            ContinueProcessing(state) 
                                        )
                                        
    let spawnAgent (f:'a -> 'b -> 'b) initialState =
        let agent = new AsyncAgent<'a,'b>(f)
        let mbox = createAgentMailbox agent initialState
        agent.InitializeMailbox mbox
        agent

    let spawnWorker f = spawnAgent (fun msg _ -> f msg;()) ()

    let spawnParallelWorker (f:'msg -> unit) howMany =
        let workers = Array.init howMany (fun _ -> spawnWorker (fun x -> f x))
        let sendAll msg = workers |> Array.iter (fun worker -> worker.PostControl msg)
        let rec orchestrator = spawnWorker (fun (msg:'msg) ->
                                    match box msg with
                                    | :? Control<'msg,unit> as c -> sendAll c
                                    | _ -> workers.[orchestrator.Index].Post msg
                                           orchestrator.Index <- (orchestrator.Index + 1) % howMany)
        orchestrator                                

    let public (<--) (a:AsyncAgent<'msg,'state>) (msg:'msg) = a.Post msg
    let public (<-!) (a:AsyncAgent<'msg,'state>) msg = a.PostControl msg

    open System.Threading.Tasks
    
    type AsyncResultCell<'T>() =
        let source = new TaskCompletionSource<'T>()
        member this.RegisterResult r = source.SetResult(r)
        member this.AsyncWaitResult =
            Async.FromContinuations(fun (cont,_,_) -> 
                let y = fun (t:Task<'T>) -> cont (t.Result)
                source.Task.ContinueWith(y) |> ignore)

    type WorkQueue() =
        let workQueue = spawnWorker (fun f -> f())
        member w.Queue (f) = workQueue <-- f
        member w.QueueWithTask f : Task<'T> =
            let source = new TaskCompletionSource<_>()
            workQueue <-- (fun () -> f() |> source.SetResult)
            source.Task
        member w.QueueWithAsync (f:unit -> 'T) : Async<'T> =
            let result = new AsyncResultCell<'T>()
            workQueue <-- (fun () -> f() |> result.RegisterResult )
            result.AsyncWaitResult
        member w.Restart () = workQueue <-! Restart
        member w.Stop () = workQueue <-! Stop
        member w.SetErrorHandler(h) =
            let managerF = fun (_, name:string, ex:Exception, _, _, _) -> h name ex                             
            let manager = spawnWorker managerF
            workQueue <-! SetManager manager
        member w.SetName(name) = workQueue <-! SetName(name)
        member w.SetQueueHandler(g) = workQueue <-! SetWorkerHandler(g)
        member w.SetTimeoutHandler(timeout, f) = workQueue <-! SetTimeoutHandler(timeout, f)
        
         
