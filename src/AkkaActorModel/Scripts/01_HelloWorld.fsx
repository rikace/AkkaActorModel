// Create Console-application, then NuGet: Install-Package Akka
module AkkaConsoleApplication

#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
//#r "../../../packages/"
#endif


open Akka.FSharp
open Akka
open Akka.Actor
open System

module EchoActor =
    
    let system = System.create "actor-system" <| Configuration.defaultConfig()

    type EchoActor() =
        inherit UntypedActor()
            override this.OnReceive (msg:obj) =
                printfn "Received Message %A" msg
                
    
    // use Props to create an Actor
    let echoActor = system.ActorOf(Props(typedefof<EchoActor>), "echo")
    echoActor <! "Hello!"

    
    let echo (msg:obj) = printfn "Received Message %A" msg
    let echoActor' = spawn system "echo" (actorOf echo)
    echoActor' <! "Hello!"

    let echo' (mailbox:Actor<'a>) =
        let rec loop () = actor {
            let! msg = mailbox.Receive()
            printfn "Received Message %A" msg
            return! loop ()
        }
        loop ()

    let echoActor'' = spawn system "echo" echo'
    echoActor'' <! "Hello!"
    
    
    let parent (mailbox:Actor<'a>) =
        let child = spawn mailbox "child" echo'
        
        let rec loop () = actor {
            let! msg = mailbox.Receive()
            printfn "Received Message %A" msg
            child.Forward msg
            return! loop ()
        }
        loop ()
        
        
    let supervisor =
        spawmnOpt system "parent" parent <|
            [ SpawnOption.SupervisorStrategy (
                    Strategy.OneForOne(fun e ->
                        match w with
                        | :? DivideByZeroException -> Directive.Restart
                        | _ -> SupervisorStrategy.DefaultDecider(e))) ]

module GreetingImperative =


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

module GreetingFunctional =
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

