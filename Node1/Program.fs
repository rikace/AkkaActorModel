open System
open Akka
open System.Linq
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open Akka.Routing
open SharedNodes

[<EntryPoint>]
let main argv = 

    Console.Title <- "NODE 1"
   
    let configRouting = Configuration.parse """
                akka {  
                    log-config-on-start = on
                    stdout-loglevel = DEBUG
                    loglevel = ERROR
                    actor {
                        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"

                        deployment {
                            /localactor {
                                router = round-robin-pool
                                nr-of-instances = 5
                            }
                            /remoteactor {
                                router = round-robin-pool
                                nr-of-instances = 5
                                remote = "akka.tcp://system2@localhost:8080"  # NODE 2
                            }
                        }
                    }
                    remote {
                        helios.tcp {
                            transport-class = "Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote"
		                    applied-adapters = []
		                    transport-protocol = tcp
		                    port = 8090
		                    hostname = localhost
                        }
                    }
                }"""


    // create a local group router (see config)
    // routing 
    let system = System.create "system1" <| configRouting

    let local = system.ActorOf<SomeActor>("localactor") // Name convention used in the config deplyment section
    

    // these messages should reach the workers via the routed local ref
    local.Tell("Local message 1")
    local.Tell("Local message 2")
    local.Tell("Local message 3")
    local.Tell("Local message 4")
    local.Tell("Local message 5")

    local <! ("Local message 6")
    local <! ("Local message 7")
    local <! ("Local message 8")
    local <! ("Local message 9")
    local <! ("Local message 10")

   
    Console.WriteLine("Press Enter to swith to the Remote Actor")
    Console.ReadLine() |> ignore 


    // create a remote deployed actor
    let remote = system.ActorOf<SomeActor>("remoteactor")

    // this should reach the remote deployed ref
    remote.Tell("Remote message 1")
    remote.Tell("Remote message 2")
    remote.Tell("Remote message 3")
    remote.Tell("Remote message 4")
    remote.Tell("Remote message 5")

    remote <! ("Remote message 6")
    remote <! ("Remote message 7")
    remote <! ("Remote message 8")
    remote <! ("Remote message 9")
    remote <! ("Remote message 10")


    Console.ReadLine() |> ignore 

    system.Shutdown()

    0 // return an integer exit code
