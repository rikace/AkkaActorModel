#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
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


let strategy =
    Strategy.OneForOne (fun e ->
        match e with
        | :? DivideByZeroException -> Directive.Resume
        | :? ArgumentException -> Directive.Stop
        | _ -> Directive.Escalate)


let supervisor =
    spawnOpt system "math-system" <| (fun mailbox ->
        // spawn is creating a child actor
        let mathActor = spawn mailbox "math-actor"

        let rec loop() = actor {
            let! msg = mailbox.Receive()
            let result = msg % 2
            match result with
            | 0 -> mailbox.Sender() <! "Even"
            | _ -> mailbox.Sender() <! "Odd"
            return! loop()
        }
        loop()) [ SupervisorStrategy(strategy) ]
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
