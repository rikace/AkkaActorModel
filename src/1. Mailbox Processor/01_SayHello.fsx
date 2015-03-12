#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif


open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

// #Using Actor
// Actors are one of Akka's concurrent models.
// An Actor is a like a thread instance with a mailbox. 
// It can be created with system.ActorOf: use receive to get a message, and <! to send a message.
// This example is an EchoServer which can receive messages then print them.

let system = ActorSystem.Create("FSharp")

type EchoServer =
    inherit Actor

    override x.OnReceive message =
        match message with
        | :? string as msg -> printfn "Hello %s" msg
        | _ ->  failwith "unknown message"

let echoServer = system.ActorOf(Props(typedefof<EchoServer>, Array.empty))

echoServer <! "F#!"

system.Shutdown()