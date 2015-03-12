open System
open System.Threading
open System.Collections.Generic

type Agent<'T> = MailboxProcessor<'T>
let (<--) (a:Agent<_>) x = a.Post x
let (-->) (a:Agent<_>) x = a.PostAndReply(fun ch -> ch.Reply())

////////////////////////// Disposable Agent /////////////////
type AgentDisposable<'T>(f:MailboxProcessor<'T> -> Async<unit>, ?cancelToken:System.Threading.CancellationTokenSource) =
    let cancelToken = defaultArg cancelToken (new System.Threading.CancellationTokenSource())
    let agent = MailboxProcessor.Start(f, cancelToken.Token)
    member x.Agent = agent
    interface IDisposable with
        member x.Dispose() = (agent :> IDisposable).Dispose()
                             cancelToken.Cancel()

////////////////////////// LinkListAsyncAgent ///////////////////// 
type internal LinkListAsyncAgentMessage<'T> = 
  | Add of 'T * AsyncReplyChannel<unit> 
  | GetFirst of AsyncReplyChannel<'T>
  | GetLast of AsyncReplyChannel<'T>
  | GetCount of AsyncReplyChannel<int>

type LinkListAsyncAgent<'T>() =
  [<VolatileField>]
  let mutable count = 0
  let agent = Agent.Start(fun agent ->    
    let link = new LinkedList<'T>()
    let rec emptyList() = 
      agent.Scan(fun msg ->
        match msg with 
        | Add(value, reply) -> Some(addAndContinue(value, reply))
        | GetCount(reply) -> reply.Reply(0)
                             None
        | _ -> None )    
    and runningList() = async {
      let! msg = agent.Receive()
      match msg with 
      | Add(value, reply) -> return! addAndContinue(value, reply)
      | GetFirst(reply) -> return! getFirstAndContinue(reply) 
      | GetLast(reply) -> return! getLastAndContinue(reply) 
      | GetCount(reply) -> reply.Reply(count)
                           return! runningList() }
    and addAndContinue (value:LinkedListNode<'T>, reply) = async {
      reply.Reply() 
      link.AddFirst(value)
      //System.Threading.Interlocked.Increment count |> ignore
      count <- link.Count
      return! chooseState() }
    and getFirstAndContinue (reply) = async {
      reply.Reply(link.First)
      link.RemoveFirst()
      //System.Threading.Interlocked.Decrement count |> ignore
      count <- link.Count
      return! chooseState() }
    and getLastAndContinue (reply) = async {
      reply.Reply(link.Last)
      link.RemoveLast()      
      //System.Threading.Interlocked.Decrement count |> ignore
      count <- link.Count
      return! chooseState() }
    and chooseState() = 
      if link.Count = 0 then emptyList()
      else runningList()
    emptyList() )

  member x.AsyncAdd(v:'T, ?timeout) =
    agent.PostAndAsyncReply((fun ch -> Add(new LinkedListNode<'T>(v), ch)), ?timeout=timeout)

  member x.AsyncAddLinkNode(v:LinkedListNode<'T>, ?timeout) =
    agent.PostAndAsyncReply((fun ch -> Add((v), ch)), ?timeout=timeout)

  member x.AsyncGetFirst(?timeout) =
    agent.PostAndTryAsyncReply(GetFirst, ?timeout=timeout) 
    
  member x.AsyncGetLast(?timeout) = 
    agent.PostAndTryAsyncReply(GetLast, ?timeout=timeout)

  member x.Count = agent.PostAndAsyncReply(GetCount)
  //Thread.VolatileRead count

////////////////////////// ThrottlingAgentMessage ///////////////////// 
type ThrottlingAgentMessage = //<'T, 'R> = 
    | Completed
    | Job of int * Async<unit>
    | JobReply of int * Async<unit> * AsyncReplyChannel<int * ResultState>
and ResultState =
    | Success
    | Fail of Exception
    | Cancel
 //   | JobResult of int * Async<'T> * AsyncReplyChannel<int * 'R>

////////////////////////// IThrottlingAgent ///////////////////// 
[<InterfaceAttribute>]
type IThrottlingAgent =
    inherit IDisposable
    abstract member DoWork : int * Async<unit> -> Async<unit>
    abstract member DoWorkAsync : int * Async<unit> -> Async<unit>
   // abstract member PostMessage : int * int * ResultState 
    abstract member Count : int with get
    abstract member Stop : unit -> unit
    abstract member ListTasks : LinkListAsyncAgent<ThrottlingAgentMessage> with get
    
////////////////////////// AgentOrchestratorBase ///////////////////// 
[<InterfaceAttribute>]
type IAgentOrchestrator =
    abstract member Workers : IThrottlingAgent array with get
    abstract member Stop : unit -> unit
    abstract member ErrorMessage : int * int * Exception -> unit

////////////////////////// TaskAgent ///////////////////// 
type TaskAgent(agentId:int, limit:int, agentOrchestrator:IAgentOrchestrator, 
                       ?timeOutMessage:int, ?cts:System.Threading.CancellationTokenSource) as self =
 // [<VolatileFieldAttribute>]
  let countWorkInProcess = ref 0 
  let guidId = Guid.NewGuid()
  let timeOutMessage = defaultArg timeOutMessage 500
  let link = LinkListAsyncAgent<ThrottlingAgentMessage>()
  let cts = defaultArg cts (new System.Threading.CancellationTokenSource())
   
  let jobError = new Event<int * System.Exception>() // the int is the job id
  let cancelled = new Event<System.OperationCanceledException>()
  let jobComleted = new Event<int>() // the int is the job id
  
  let syncCtx = System.Threading.SynchronizationContext.Current                            
  do match syncCtx with
    | null -> failwith ""
    | _ -> ()

  let raiseEventFunc (event:Event<_>) args = //(raiseEventFunc event args )()
        let ctx = System.Threading.SynchronizationContext.Current
        (fun _ -> match ctx with
                  | null -> event.Trigger args
                  | _ -> ctx.Post(SendOrPostCallback(fun _ -> event.Trigger args), state=null) )
    
  let raiseEvent (event:Event<_>) args =
        //(raiseEventFunc event args )()
        //syncCtx.Post(SendOrPostCallback(fun _ -> event.Trigger args), state=null)
        event.Trigger args
                                                                
  let taskAgent = (fun (agent:MailboxProcessor<ThrottlingAgentMessage>) -> 
    let rec waiting () = 
      agent.Scan(function
        | Completed -> Some(working (limit - 1))
        | _ -> None)
    and working count = async { 
      let! msg = link.AsyncGetFirst(1000)//timeOutMessage)
      match msg with 
      | None -> let taskCandidate = agentOrchestrator.Workers |> Array.filter(fun (t:IThrottlingAgent) -> t.Count > 0)
                match taskCandidate with               
                | arr when arr.Length > 0 -> 
                                let! job = arr.[0].ListTasks.AsyncGetLast(timeOutMessage) 
                                if job.IsSome then do! link.AsyncAddLinkNode(job.Value)
                                return! working(count)
                | _ -> return! working(count)
      | Some(c) -> match c.Value with 
                   | Completed       ->  return! working (count - 1)
//                   | JobResult(id, work, reply) -> 
//                            let job = async { try
//                                                let! res = work
//                                                reply.Reply(res, id)
//                                                finally
//                                                agent.Post(Completed) }
//                            job |> Async.Start
//                            return! chooseState(count)
                   | Job(id, work)   ->  let job =  async { try 
                                                                // printfn "count I %d " count
                                                                do! work
                                                           finally 
                                                              agent.Post(Completed) }
                                         job |> Async.Start
                                         return! chooseState(count)
                   | JobReply (id, work, reply) ->   let job = async { try 
                                                                         do! work
                                                                     finally agent.Post(Completed) } 
                                                     Async.StartWithContinuations(job,
                                                                (fun completed -> reply.Reply(id, Success)
                                                                                  raiseEvent jobComleted id),
                                                                (fun (exn:Exception) -> reply.Reply(id, Fail exn)
                                                                                        agentOrchestrator.ErrorMessage(agentId, id, exn)
                                                                                        raiseEvent jobError (id, exn)),
                                                                (fun (cancel:OperationCanceledException) -> reply.Reply(id, Cancel)
                                                                                                            raiseEvent cancelled cancel),
                                                                cts.Token)
                                                     return! chooseState(count)  }
    and chooseState count = async {    
        if count < limit then return! working (count + 1)
        else return! waiting () }
    working 0) 

  let agent = 
    let agentDisposable = new AgentDisposable<ThrottlingAgentMessage>(taskAgent, cts)
    agentDisposable.Agent.Error.Add(fun (error:Exception) -> ())
    agentDisposable

  interface IComparable<TaskAgent> with
      member x.CompareTo(task) =
        
        (task :> IThrottlingAgent).Count.CompareTo((self :> IThrottlingAgent).Count)
  
  override self.Equals(o) = match o with
                            :? TaskAgent -> let taskObj = (o :?> TaskAgent)                                           
                                            (self.ID = taskObj.ID && self.GuidId = taskObj.GuidId)
                            | _ -> false
    
  override self.GetHashCode() = agentId.GetHashCode() ^^^ guidId.GetHashCode()    
  member x.Count with get() = Async.RunSynchronously link.Count                   
  member x.ID with get() = agentId                                                
  member x.GuidId with get() = guidId                                             

  interface IComparable with
      member x.CompareTo(task) =
        self.GuidId.CompareTo((task :?> TaskAgent).GuidId)

  interface IThrottlingAgent with
      member x.ListTasks with get() = link
      member x.DoWork(id, work) = link.AsyncAdd(Job(id, work))
      member x.DoWorkAsync(id, work) = async { let! res = agent.Agent.PostAndAsyncReply(fun ch -> JobReply(id, work, ch)) 
                                               () }    // TODO ADD TO LIST    

                                                
      member x.Stop() = cts.Cancel()                                            
      member x.Count with get() =  Async.RunSynchronously link.Count

  interface IDisposable with
      member x.Dispose() = (agent :> IDisposable).Dispose()

////////////////////////// AgentOrchestrator /////////////////////                                   
type AgentOrchestrator(?nunProc:int, ?cts:System.Threading.CancellationTokenSource) as self =
    let numThreadsPerProc = 2
    let timeOutMessage = 500
    let cts = defaultArg cts (new System.Threading.CancellationTokenSource())
    let nunProc = defaultArg nunProc 8 //Environment.ProcessorCount        
    let workers =  Array.init nunProc (fun i -> new TaskAgent(i, 2, (self :> IAgentOrchestrator), timeOutMessage, cts))

    let agentWorker = 
        let agentWorkerDisposable = new AgentDisposable<_>((fun inbox ->
                                                let rec loop i = async {
                                                    let! msg = inbox.TryReceive(-1)
                                                    match msg with
                                                    | None -> ()
                                                    | Some(msg) ->  let mintaskCandidate = workers |> Array.minBy (fun t -> t.Count)
                                                                    let taskCandidate = (mintaskCandidate :> IThrottlingAgent)
                                                                    Async.StartWithContinuations(taskCandidate.DoWork(msg),
                                                                                                 (fun comp -> printfn "Job Completed Agent ID %d - JobId %d" mintaskCandidate.ID (fst msg)),
                                                                                                 (fun exn -> ()),
                                                                                                 (fun cancel -> ()))
                                                    return! loop ((i+1) % nunProc)  }
                                                loop 0), cts)
        agentWorkerDisposable.Agent.Error.Add(fun (error:Exception) -> ())
        agentWorkerDisposable

    member sef.Workers with get() = workers    
    member self.Post(x) = agentWorker.Agent.Post(x)
    member self.PostWork(work, ?jobId:int) = 
            let jobId = defaultArg jobId 1 // ??
            agentWorker.Agent.Post(work)

    interface IAgentOrchestrator with
        member self.Workers with get() = workers |> Array.map (fun f -> (f :> IThrottlingAgent))
        member self.Stop() =  workers |> Array.iter (fun ag -> (ag :> IDisposable).Dispose())
                              (agentWorker :> IDisposable).Dispose()

        member self.ErrorMessage(idJob:int, idAgent:int, error:Exception) = ()


let ao = new AgentOrchestrator()

ao.Post(4, async{ System.Threading.Thread.Sleep(5000) })

for i in 1..40 do ao.Post(i, async{ System.Threading.Thread.Sleep(10000) })


/////////////////////////////////


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

        static member public SpawnParallelWorker(messageHandler, howMany, ?timeout, ?timeoutHandler, ?errorHandler) =
            let timeout = defaultArg timeout (fun () -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun _ -> ContinueProcessing(()))
            let errorHandler = defaultArg errorHandler (fun _ _ -> ContinueProcessing(()))
            MailboxProcessor<'a>.SpawnAgent((fun msg (workers:MailboxProcessor<'a> array, index) ->
                                                workers.[index].Post msg
                                                (workers, (index + 1) % howMany))  
                                            , (Array.init howMany (fun _ -> MailboxProcessor<'a>.SpawnWorker(messageHandler, timeout, timeoutHandler, errorHandler)), 0))

let tprintfn s = printfn "Executing %s on thread %i" s Thread.CurrentThread.ManagedThreadId
let paralleltprintfn s =
    printfn "Executing %s on thread %i" s Thread.CurrentThread.ManagedThreadId
    Thread.Sleep(300)

let echocc = MailboxProcessor<_>.SpawnWorker(tprintfn)
let echos = MailboxProcessor.SpawnParallelWorker(paralleltprintfn, 10)

let messages = ["a";"b";"c";"d";"e";"f";"g";"h";"i";"l";"m";"n";"o";"p";"q";"r";"s";"t"]
printfn "...Just one guy doing the work"
messages |> Seq.iter (fun msg -> echocc.Post(msg))
Thread.Sleep 1000
printfn "...With a little help from his friends"
messages |> Seq.iter (fun msg -> echos.Post(msg))

///////////////////////////////////////////////////////////////////////////////

type msg1 = Message1 | Message2 of int | Message3 of string | Message4 of int * AsyncReplyChannel<int>
            
let a = MailboxProcessor.SpawnParallelWorker(function
                | Message1 -> printfn "Message1";
                | Message2 n -> printfn "Message2 %i" n;
                | Message3 _ -> failwith "I failed"
                | Message4(i,s) -> printfn "Message4 %d" i;
                                   s.Reply((i*2))
                , 10
                , errorHandler = (fun ex _ -> printfn "%A" ex; ContinueProcessing()))

                                                               
a.Post(Message1)

a.Post(Message2(100))
a.Post(Message3("abc"))
a.Post(Message2(100))


let echosw = MailboxProcessor<_>.SpawnWorker(fun msg -> printfn "%s" msg)
echosw.Post("Hello")

let counter1 = MailboxProcessor.SpawnAgent((fun i (n:int) -> printfn "n = %d, waiting..." n; n + i), 0)
counter1.Post(10)
counter1.Post(30)
counter1.Post(20)

type msg = Increment of int | Fetch of AsyncReplyChannel<int> | Stop

exception StopException

type CountingAgent() =
    let counter = MailboxProcessor.SpawnAgent((fun msg n ->
                    match msg with
                    | Increment m ->  n + m
                    | Stop -> raise(StopException)
                    | Fetch replyChannel ->
                        do replyChannel.Reply(n)
                        n
                  ), 0, errorHandler = (fun _ _ _ -> StopProcessing))
    member a.Increment(n) = counter.Post(Increment(n))
    member a.Stop() = counter.Post(Stop)
    member a.Fetch() = counter.PostAndReply(fun replyChannel -> Fetch(replyChannel))    
        
let counter2 = CountingAgent()
counter2.Increment(1)
counter2.Fetch()
counter2.Increment(2)
counter2.Fetch()
counter2.Stop()    

////////////////////////////////////////////////////////////////////////////



