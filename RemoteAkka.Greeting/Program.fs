open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System
open Akka.Configuration

type Greet(who:string) =
    member x.Who = who
 
type GreetingActor() as g =
    inherit ReceiveActor()
    do g.Receive<Greet>(fun (greet:Greet) -> 
            printfn "Hello %s" greet.Who)


[<EntryPoint>]
let main argv = 

      let config =
        Configuration.parse
            @"akka {
                actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                remote.helios.tcp {
                    hostname = localhost
                    port = 8088
                }
            }"

      let system = ActorSystem.Create("greeter", config)
            
      Console.ReadLine() |> ignore

      system.Shutdown()

//    use greetingServer =  System.create "greeting-system" configGreeing
//    let greetingActor = greetingServer.ActorOf<GreetingActor>("greeter")
//    
//    printfn "Remote GreetingActor %s listening..." greetingServer.Name     

      System.Console.ReadLine() |> ignore

      system.Shutdown()

      0 // return an integer exit code
