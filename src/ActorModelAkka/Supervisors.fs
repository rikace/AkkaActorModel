module Supervisors



#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.FSharp
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp


let strategy =
    Strategy.OneForOne (fun e ->
        match e with
        | :? DivideByZeroException -> Directive.Resume
        | :? ArgumentException -> Directive.Stop
        | _ -> Directive.Escalate)


let system = ActorSystem.Create("system")


let supervisor =
    spawnOpt system "math-system" (fun mailbox ->
        // spawn is creating a child actor
        let mathActor = spawn mailbox "math-actor" 

        let rec loop() =
            actor {
                let! msg = mailbox.Receive()
                let result = msg % 2
                match result with
                | 0 -> mailbox.Sender() <! "Even"
                | _ -> mailbox.Sender() <! "Odd"
                return! loop()
            }
        loop()) [ SupervisorStrategy(strategy) ]

