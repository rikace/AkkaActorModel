// Create Console-application, then NuGet: Install-Package Akka
module AkkaConsoleApplication

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System

type ActorMsg =
    | Greet of who:string
    | Push of int
    | Calculate


let system = System.create "MySystem" <| Configuration.load()

    
// functional 
let myActor = 
    // the function spawn instantiate an ActorRef
    // spawn attaches the behavior to our system and returns an ActorRef
    // We can use ActorRef to pass messages

    // ActorFactory -> Name -> f(Actor<Message> -> Cont<'Message, 'return>) -> ActorRef
    spawn system "Greeter-Functional"
    <| fun mailbox ->
        let rec loop state = actor { // tail recursive function,
                                 // which uses an actor { ... } computation expression 
            let! msg = mailbox.Receive()
            match msg with
            | Greet(w) -> printfn "Hello %s" w    
            | Push(value) -> return! loop (value::state)
            | Calculate -> state |> List.reduce(+) |> printfn "%d"
                           return! loop []
            }
        loop []

myActor.Tell (Push 10)
myActor <! (Push 5)
for i = 0 to 10 do myActor.Tell (Push i)
myActor <! Calculate
myActor <! Greet("Hello AKKA.Net!!")



let actor = select "akka://MySystem/user/Greeter-Functional" system
actor <! Greet("AKKA.Net!!")


system.Shutdown()


// #Using Actor
// Actors are one of Akka's concurrent models.
// An Actor is a like a thread instance with a mailbox. 
// It can be created with system.ActorOf: use receive to get a message, and <! to send a message.
// This example is an EchoServer which can receive messages then print them.

