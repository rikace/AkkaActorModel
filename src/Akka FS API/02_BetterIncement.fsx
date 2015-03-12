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


    
let system = ActorSystem.Create "example3"

let actor = spawn system "actor" <| fun mailbox ->
                let rec loop state =
                    actor {
                        let! msg = mailbox.Receive ()
                        match msg with
                        | Increment -> return! loop (state + 1)
                        | Print -> printfn "%i" state
                                   return! loop state
                    }
                loop 0

let actorSelection = select "/user/actor" system

actorSelection <! Increment

actor <! Print
for i in 1..10 do
    actor <! Increment
actor <! Print

