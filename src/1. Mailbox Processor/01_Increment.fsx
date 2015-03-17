open Akka.Actor

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System

type Message =
    | Increment
    | Print

type OtherMessage =
    | Decrement
    | Display

type SimpleActor () as this =
    inherit ReceiveActor () // Untyped

    let state = ref 0 // mutable is safe!!

    do
        // Specialize receive
        this.Receive<Message>(fun m -> 
                                match m with
                                | Print -> printfn "%i" !state
                                | Increment -> state := !state + 1)

        this.Receive<OtherMessage>(fun m -> 
                                match m with
                                | Display -> printfn "%i" !state
                                | Decrement -> state := !state - 1)


    override this.Unhandled(msg) = // can be used for dead letter
            printfn "What shoudl I do with this thing %A" (msg.GetType())



let system = ActorSystem.Create("example2")  

let actor = system.ActorOf<SimpleActor>()

actor.Tell Print // Message
actor.Tell Increment
actor.Tell Increment
actor.Tell Increment
actor.Tell Print

actor <! Decrement  // OtherMessage
actor <! Decrement
actor <! Display

actor <! "ciao"




