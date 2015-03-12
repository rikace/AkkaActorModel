// Create Console-application, then NuGet: Install-Package Akka
module AkkaConsoleApplication

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System

type GreetMsg =
    | Greet of who:string

type Greet(who:string) =
    member x.Who = who
 
type GreetingActor() as g =
    inherit ReceiveActor()
    do g.Receive<Greet>(fun (greet:Greet) -> 
            printfn "Hello %s" greet.Who)


type HelloServer =
    inherit Actor

    override x.OnReceive message =
        match message with
        | :? string as msg -> printfn "Hello %s" msg
        | _ ->  failwith "unknown message"


[<EntryPoint>]   
let main argv = 
    let system = ActorSystem.Create "MySystem"
    
    let greeter = system.ActorOf<GreetingActor> "greeter"
    Greet("World") |> greeter.Tell
    


    // More functional 
    let greeter = // the function spawn instantiate an ActorRef
        // spawn attaches the behavior to our system and returns an ActorRef
        // We can use ActorRef to pass messages
        spawn system "Greeter-Functional"
        <| fun mailbox ->
            let rec loop() = actor { // tail recursive function, which uses an actor { ... } computation expression 
                let! msg = mailbox.Receive()
                match msg with
                | Greet(w) ->printfn "Hello %s" w                
                return! loop() }
            loop()

    greeter <! GreetMsg.Greet("AKKA.Net!!")


    let echoServer = system.ActorOf(Props(typedefof<HelloServer>, Array.empty))
    echoServer <! "F#!"

    let echoServer = 
        spawn system "EchoServer"
        <| fun mailbox ->
                actor {
                    let! message = mailbox.Receive()
                    match box message with
                    | :? string as msg -> printfn "Hello %s" msg
                    | _ ->  failwith "unknown message"
                } 

    echoServer <! "F#!"



    system.Shutdown()

    0 // return an integer exit code

// #Using Actor
// Actors are one of Akka's concurrent models.
// An Actor is a like a thread instance with a mailbox. 
// It can be created with system.ActorOf: use receive to get a message, and <! to send a message.
// This example is an EchoServer which can receive messages then print them.

