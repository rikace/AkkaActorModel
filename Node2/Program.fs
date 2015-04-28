open System
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open SharedNodes

[<EntryPoint>]
let main argv = 
    
    Console.Title <- "NODE 2"
    
    let config = Configuration.parse """
            akka {  
                log-config-on-start = on
                stdout-loglevel = DEBUG
                loglevel = ERROR
                actor {
                    provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
        
                }
                remote {
                    helios.tcp {
                        transport-class = "Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote"
		                applied-adapters = []
		                transport-protocol = tcp
		                port = 8080
		                hostname = localhost
                    }
                }
            }"""

    let system = System.create "system2" config


    Console.ReadLine() |> ignore
    system.Shutdown()

    0 
    
