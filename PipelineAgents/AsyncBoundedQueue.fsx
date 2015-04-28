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


