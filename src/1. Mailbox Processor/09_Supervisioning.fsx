#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka
open Akka.FSharp
open Akka.Actor
open System

type Message =
    | Boom
    | Print of string


let system = ActorSystem.Create "example3"

let options = [SpawnOption.SupervisorStrategy(Strategy.OneForOne(fun e -> Directive.Restart))]

let strategy =
    Strategy.OneForOne (fun e ->
        match e with
        | :? DivideByZeroException -> Directive.Resume
        | :? ArgumentException -> Directive.Stop
        | _ -> Directive.Escalate)


let actor = spawnOpt system "actor" <|
                        fun mailbox ->
                            let rec loop () =
                                actor {
                                    let! msg = mailbox.Receive ()
                                    match msg with
                                    | Boom -> raise <| Exception("Oops")
                                    | Print s -> printfn "%s" s
                                                 return! loop ()
                                }
                            loop ()
                        <| options

actor <! Print("Hello")
actor <! Boom
actor <! Print("I'm back")


