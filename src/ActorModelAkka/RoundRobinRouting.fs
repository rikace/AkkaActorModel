module RoundRobinRouting

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open System.Linq
open Akka
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Routing
open Akka.Configuration

let config = Configuration.parse """
                    akka {  
                        actor {
                            deployment {
                                /localactor {
                                    router = round-robin-pool
                                    nr-of-instances = 2
                                }
                            }
                        }
                    }"""

let system = System.create "system1" <| config //Configuration.load()

let local = spawnOpt system "localactor" (fun mailbox ->
        let address = mailbox.Self.Path.ToStringWithAddress()   
        let rec loop() = actor {
            let! msg = mailbox.Receive()
            Console.WriteLine("{0} got {1} - Thread #id {2}", address, msg, System.Threading.Thread.CurrentThread.ManagedThreadId)
            return! loop()
            }
        loop()) [ (SpawnOption.Router(RoundRobinPool 2)) ]


//these messages should reach the workers via the routed local ref
local.Tell("Local message 1")
local.Tell("Local message 2")
local.Tell("Local message 3")
local.Tell("Local message 4")
local.Tell("Local message 5")
local.Tell("Local message 6")
local.Tell("Local message 7")
local.Tell("Local message 8")

system.Shutdown()


