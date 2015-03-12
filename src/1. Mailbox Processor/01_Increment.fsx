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

type SimpleActor () as this =
    inherit ReceiveActor ()

    let state = ref 0 // mutable is safe!!

    do
        this.Receive<Message>(fun m -> match m with
                                       | Print -> printfn "%i" !state
                                       | Increment -> state := !state + 1)


let system = ActorSystem.Create("example2")  

let actor = system.ActorOf<SimpleActor>()

actor.Tell Print
actor.Tell Increment
actor.Tell Increment
actor.Tell Increment
actor.Tell Print

