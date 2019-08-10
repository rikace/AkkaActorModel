#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System

type IncrementMessage =
    | Increment
    | Print

type Decrementessage =
    | Decrement
    | Display

type SimpleActor() as this =
    inherit ReceiveActor()

    let state = ref 0 // mutable is safe!!

    do
        // Specialize receive
        this.Receive<IncrementMessage>(fun m ->
                                match m with
                                | Print -> printfn "%i" !state
                                | Increment -> state := !state + 1)

        this.Receive<Decrementessage>(fun m ->
                                match m with
                                | Display -> printfn "%i" !state
                                | Decrement -> state := !state - 1)


    override this.Unhandled(msg:obj) = // can be used for dead letter
            printfn "What shoudl I do with this thing %A" (msg.GetType())



let system = ActorSystem.Create("example2")

let actor = system.ActorOf<SimpleActor>("SimpleActor")

actor.Tell Print // Message
actor.Tell Increment
actor.Tell Increment
actor.Tell Increment
actor.Tell Print

actor <! Decrement  // OtherMessage
actor <! Decrement
actor <! Display

let actorSelection = select "/user/SimpleActor" system

actorSelection <! Increment

actor <! "ciao"




