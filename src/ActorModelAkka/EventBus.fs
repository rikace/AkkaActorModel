module EventBus


#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.FSharp
open Akka.Actor
open Akka.Configuration

// Event Bus
// Send messages to groups of actors
// It is used primary for log messages and Dead Letters,but it can be used by the user code for other purposes as well

let system = ActorSystem.Create("eventBus-System")

let eventBus = 
    spawn system "EventBus"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                match box message with
                | :? int -> 
                    printfn "Echo I received a number and I am duplicate it '%d'" ((int message) * 2)
                    return! loop()

                | :? string -> 
                    printfn "Echo '%s'" (string message)
                    return! loop()


                | _ ->  failwith "unknown message"
            } 
        loop()

let eventStream = system.EventStream

eventStream.Subscribe(eventBus, typedefof<string>) |> ignore

eventStream.Publish("Hello F#!!")
eventStream.Publish("Hello Akka.Net")


eventStream.Subscribe(eventBus, typedefof<int>) |> ignore


eventStream.Publish(7)

system.Shutdown()

