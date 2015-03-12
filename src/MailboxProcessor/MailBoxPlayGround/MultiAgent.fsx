namespace MultiAgents

open System
open System.Threading
open System.Collections.Generic
open Microsoft.FSharp.Control

[<AutoOpenAttribute>]
module MultiAgentsMoodule =
    type AfterError<'state> =
        | ContinueProcessing of 'state
        | StopProcessing
        | RestartProcessing

    type MailboxProcessor<'a> with
        static member public SpawnAgent<'b>(messageHandler : 'a -> 'b -> 'b, 
                // a function to execute when a message comes in,
                // it takes the message and the current state as
                // parameters and returns the new state.
                                            initialState : 'b, ?timeout : 'b -> int,
                                            ?timeoutHandler : 'b -> AfterError<'b>, 
                                            // a function that is executed whenever a
                                            // timeout occurs. It takes as a parameter the current state and
                                            // returns one of AfterError
                                            ?errorHandler : 
                                            // a function that gets call if an exception is generated inside the
                                            // messageHandler function. It takes the exception, the message, the
                                            // current state and returns AfterError
                                                            Exception -> 'a option -> 'b -> AfterError<'b>) 
                                            : MailboxProcessor<'a> =
            let timeout = defaultArg timeout (fun _ -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun state -> ContinueProcessing(state))
            let errorHandler = defaultArg errorHandler (fun _ _ state -> ContinueProcessing(state))
            MailboxProcessor.Start(fun inbox ->
                let rec loop (state) =
                    async {
                        let! msg = inbox.TryReceive(timeout (state))
                        try
                            match msg with
                            | None ->
                                match timeoutHandler state with
                                | ContinueProcessing(newState) -> return! loop (newState)
                                | StopProcessing -> return ()
                                | RestartProcessing -> return! loop (initialState)
                            | Some(m) -> return! loop (messageHandler m state) // Execution of message here
                        with ex ->
                            match errorHandler ex msg state with
                            | ContinueProcessing(newState) -> return! loop (newState)
                            | StopProcessing -> return ()
                            | RestartProcessing -> return! loop (initialState)
                    }
                loop (initialState))

    type MailboxProcessor<'a> with
        // Stateless Agent, simple version of SpawnAgent
        static member public SpawnWorker(messageHandler, ?timeout, ?timeoutHandler, ?errorHandler) =
            let timeout = defaultArg timeout (fun () -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun _ -> ContinueProcessing(()))
            let errorHandler = defaultArg errorHandler (fun _ _ -> ContinueProcessing(()))
            MailboxProcessor.SpawnAgent((fun msg _ ->
                                        messageHandler msg
                                        ()), (), timeout, timeoutHandler, (fun ex msg _ -> errorHandler ex msg))

    type MailboxProcessor<'a> with
        // Create and run a whole bunch of Agents at the same time (execute each messageHandler in parallel)
        static member public SpawnParallelWorker(messageHandler, howMany, ?timeout, ?timeoutHandler, ?errorHandler) =
            let timeout = defaultArg timeout (fun () -> -1)
            let timeoutHandler = defaultArg timeoutHandler (fun _ -> ContinueProcessing(()))
            let errorHandler = defaultArg errorHandler (fun _ _ -> ContinueProcessing(()))
            MailboxProcessor<'a>
                .SpawnAgent((fun msg (workers : MailboxProcessor<'a> array, index) ->
                            workers.[index].Post msg
                            (workers, (index + 1) % howMany)),

                            (Array.init howMany
                                 (fun _ ->
                                 MailboxProcessor<'a>.SpawnWorker(messageHandler, timeout, timeoutHandler, errorHandler)),
                             0))

// One drawback with the current code is that it doesn’t supports cancellations
(********************************************************************************************************
 ******************************* T E S T ****************************************************************
 ********************************************************************************************************)
// ContinueAgent from the Expert F# book
module TestSpawnAgent =
    type msg =
        | Increment of int
        | Fetch of AsyncReplyChannel<int>
        | Stop

    exception StopException

    type CountingAgent() =

        let counter =
            MailboxProcessor.SpawnAgent((fun msg n ->
                                        match msg with
                                        | Increment m -> n + m
                                        | Stop -> raise (StopException)
                                        | Fetch replyChannel ->
                                            do replyChannel.Reply(n)
                                            n), 0, errorHandler = (fun _ _ _ -> StopProcessing))

        member a.Increment(n) = counter.Post(Increment(n))
        member a.Stop() = counter.Post(Stop)
        member a.Fetch() = counter.PostAndReply(fun replyChannel -> Fetch(replyChannel))

    let counter' = CountingAgent()

    counter'.Increment(1)
    counter'.Fetch()
    counter'.Increment(2)
    counter'.Fetch()
    counter'.Stop()

    let echo = MailboxProcessor<_>.SpawnWorker(fun msg -> printfn "%s" msg)

    echo.Post("Hello")
    echo.Post("World")
    echo.Post("Hello guys!")


    type msg1 =
        | Message1
        | Message2 of int
        | Message3 of string

    let a = MailboxProcessor.SpawnParallelWorker(function
                    | Message1 -> printfn "Message1 - Thread Id #%d" Thread.CurrentThread.ManagedThreadId
                    | Message2 n -> printfn "Message2 %i - Thread Id #%d" n Thread.CurrentThread.ManagedThreadId
                    | Message3 _ -> failwith "I failed"
                    , 10 (* 10 agents *)
                    , errorHandler = (fun ex _ -> printfn "%A" ex; ContinueProcessing()))


    a.Post(Message1)
    a.Post(Message3("abc")) // Error
    a.Post(Message2(100))
    a.Post(Message2(100))


    let b = MailboxProcessor.SpawnParallelWorker((fun s -> printfn "%s running on thread %i" s Thread.CurrentThread.ManagedThreadId), 10)
                    

    let messages = ["a";"b";"c";"d";"e";"f";"g";"h";"i";"l";"m";"n";"o";"p";"q";"r";"s";"t"]
    messages |> Seq.iter (fun msg -> b.Post(msg))
   
   