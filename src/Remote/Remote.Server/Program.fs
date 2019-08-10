
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System
open GreetingModule

[<EntryPoint>]
let main argv =

    let config = Configuration.parse """
                    akka {
                        actor {
                            provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
                        }
                        remote {
                            helios.tcp {
                               transport-protocol = tcp
                               port = 9233
                               hostname = localhost
                            }
                        }
                    }"""

    let system = System.create "MyServer" config

    let _ = system.ActorOf<GreetingActor>("greeter")

    Console.ReadLine() |> ignore

    system.Dispose()
    0
