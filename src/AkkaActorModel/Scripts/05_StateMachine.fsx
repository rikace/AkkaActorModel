module StateMachine

#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
#endif


open System
open Akka.FSharp
open Akka.Actor


// State Machine

type ActorMsg =
    | Hello of string
    | Hi

let system = ActorSystem.Create("FSharp")

let echoServer =
    spawn system "EchoServer"
    <| fun mailbox ->
        let rec replyUa() =
            actor {
                let! message = mailbox.Receive()
                match message with
                | Hello name -> printfn "Привіт %s" name
                | Hi -> printfn "Привіт!"

                return! replySw()
            }
        and replySw() =
            actor {
                let! message = mailbox.Receive()
                match message with
                | Hello name -> printfn "Hallå %s" name
                | Hi -> printfn "Hallå!"

                return! replyUa()
            }

        replyUa()

echoServer <! Hello "Kyiv F# group!"
echoServer <! Hello "Akka.NET team!"

system.Dispose()
