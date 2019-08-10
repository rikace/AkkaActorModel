module RemoteDeploy

#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
#endif

open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System
open System.IO


System.Diagnostics.Process.Start(@"C:\Git\AkkaActorModel\RemoteAkka.Server.Console\bin\Debug\RemoteAkka.Server.Console.exe") |> ignore

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
                    }
                    """

let system = System.create "MyClient" config

//get a reference to the remote actor
let greeter = select "akka.tcp://MyServer@localhost:9233/user/greeter" system

//let greeter = system
//        .ActorSelection("akka.tcp://MyServer@localhost:8080/user/greeter")

//send a message to the remote actor
greeter <! new Greet("Akka.NET & F#!!")



