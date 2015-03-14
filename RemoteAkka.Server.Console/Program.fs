
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System

[<EntryPoint>]
let main argv = 
    
    let config = ConfigurationFactory.ParseString(@"
                    akka {
                        actor {
                            provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        }

                        remote {
                            helios.tcp {
                                port = 8080
                                hostname = localhost
                            }
                        }
                    }")

    let system = ActorSystem.Create("MyServer", config)

    let _ = system.ActorOf<GreetingActor>("greeter")
    
    Console.ReadLine() |> ignore

    system.Shutdown()
    0