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

// An ActorSystem is a reference to the underlying system and Akka.NET framework. 
// All actors live within the context of this actor system
let system = ActorSystem.Create("FSharp")

type EchoServer =
    inherit Actor // UntypedActor

    override x.OnReceive (message:obj) =
        match message with
        | :? string as msg -> printfn "Hello %s" msg
        | _ ->  printfn "What shoudl I do with this thing %A" (message.GetType())
                //failwith "unknown message"

let echoServer = system.ActorOf(Props(typedefof<EchoServer>)) // Name ??

echoServer.Tell 42
echoServer.Tell "LambdaConf 2015 is awesome!"

system.Shutdown()





