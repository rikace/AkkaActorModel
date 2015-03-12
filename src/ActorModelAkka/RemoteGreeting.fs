module RemoteGreeting

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System


let configGreeing = Configuration.parse  
                     @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 8088
            }
        }"


type Greet(who:string) =
    member x.Who = who
 
type GreetingActor() as g =
    inherit ReceiveActor()
    do g.Receive<Greet>(fun (greet:Greet) -> 
            printfn "Hello %s" greet.Who)


let system = ActorSystem.Create("myClient", configGreeing)

let greetingActor = system.ActorSelection("akka.tcp://greeting-system@localhost:8088/user/greeter")

greetingActor <! Greet("Ricky")


     

