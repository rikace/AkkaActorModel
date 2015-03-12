#load "CommonModule.fsx"
namespace AsyncBoundedQueue

open Microsoft.FSharp.Control
open System.Collections.Generic
open Common
open System.Threading

[<AutoOpenAttribute>]
module AsyncQueue =

    // represent a queue operation
    type Instruction<'T> =
        | Enqueue of 'T * (unit -> unit) 
        | Dequeue of ('T -> unit)

    type AsyncBoundedQueue<'T> (capacity: int, ?cancellationToken:CancellationTokenSource) =
        let waitingConsumers, elts, waitingProducers = Queue(), Queue<'T>(), Queue()
        let cancellationToken = defaultArg cancellationToken (new CancellationTokenSource())

(*  The following balance function shuffles as many elements through the queue 
    as possible by dequeuing if there are elements queued and consumers waiting 
    for them and enqueuing if there is capacity spare and producers waiting *)
        let rec balance() =
            if elts.Count > 0 && waitingConsumers.Count > 0 then
                elts.Dequeue() |> waitingConsumers.Dequeue()
                balance()
            elif elts.Count < capacity && waitingProducers.Count > 0 then
                let x, reply = waitingProducers.Dequeue()
                reply()
                elts.Enqueue x
                balance()

(*  This agent sits in an infinite loop waiting to receive enqueue and dequeue instructions, 
    each of which are queued internally before the internal queues are rebalanced *)
        let agent = MailboxProcessor.Start((fun inbox ->
                let rec loop() = async { 
                        let! msg = inbox.Receive()
                        match msg with
                        | Enqueue(x, reply) -> waitingProducers.Enqueue (x, reply)
                        | Dequeue reply -> waitingConsumers.Enqueue reply
                        balance()
                        return! loop() }
                loop()), cancellationToken.Token)

        member __.AsyncEnqueue x =
              agent <-! (fun reply -> Enqueue(x, reply.Reply))
        member __.AsyncDequeue() =
              agent <-! (fun reply -> Dequeue reply.Reply)

        interface System.IDisposable with          
              member __.Dispose() = 
                cancellationToken.Cancel()
                (agent :> System.IDisposable).Dispose()


(********************************************************************************************************
 ******************************* T E S T ****************************************************************
 ********************************************************************************************************)
module TestAsyncBoundedQueue =
    let test() = 
        for _ in [0..5] do
            let n = 3000
            use queue = new AsyncBoundedQueue<_>(10)
            [ async { let timer = System.Diagnostics.Stopwatch.StartNew()
                      for i in 1..n do
                        do! queue.AsyncEnqueue i
                      return "Producing", float n / timer.Elapsed.TotalSeconds }
              async { let timer = System.Diagnostics.Stopwatch.StartNew()
                      for i in 1..n do
                        let! _ = queue.AsyncDequeue()
                        while timer.Elapsed.TotalMilliseconds < float i do
                          do! Async.Sleep 1
                      return "Consuming", float n / timer.Elapsed.TotalSeconds } ]
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Seq.iter (fun (s, t) -> printfn "%s at %0.0f msgs/s" s t)
    
    //test()       
        
