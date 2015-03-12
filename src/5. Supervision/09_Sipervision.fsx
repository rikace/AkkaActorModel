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

let options = [SpawnOption.SupervisorStrategy(Strategy.OneForOne (fun e -> Directive.Restart))]

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

//
//
//open Akka.Actor
//open Akka.FSharp
//open System
//
//type Message =
//    | Boom
//    | Print of string
//
//
//[<EntryPoint>]
//let main argv = 
//    
//    let system = ActorSystem.Create "example3"
//
//    let options = [SupervisorStrategy(Strategy.oneForOne (fun e -> Directive.Restart))]
//
//    let actor = spawnOpt system "actor" <|
//        fun mailbox ->
//            let rec loop () =
//                actor {
//                    let! msg = mailbox.Receive ()
//                    match msg with
//                    | Boom -> raise <| Exception("Oops")
//                    | Print s -> printfn "%s" s
//                    return! loop ()
//                }
//            loop ()
//        <| options
//
//    actor <! Print("Hello")
//    actor <! Boom
//    actor <! Print("I'm back")
//
//    System.Console.ReadLine () |> ignore
//
//    0 // return an integer exit code
