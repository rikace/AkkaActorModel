module RemoteDeploy

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#r @"..\..\RemoteAkka.Shared\bin\Debug\RemoteAkka.Shared.dll"
#endif

open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System
open System.IO


System.Diagnostics.Process.Start(@"C:\Demo\ActorModel\ActorModelAkka\RemoteAkka.Server.Console\bin\Debug\RemoteAkka.Server.Console.exe") |> ignore


let config = ConfigurationFactory.ParseString(@"
                    akka {
                        actor {
                            provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        }
                        remote {
                            helios.tcp {
                                port = 8090
                                hostname = localhost
                            }
                        }
                    }")

let system = ActorSystem.Create("MyClient", config)

//get a reference to the remote actor
let greeter = select "akka.tcp://MyServer@localhost:8080/user/greeter" system

//let greeter = system
//        .ActorSelection("akka.tcp://MyServer@localhost:8080/user/greeter")

//send a message to the remote actor
greeter <! new Greet("Akka.NET & F#!!")



