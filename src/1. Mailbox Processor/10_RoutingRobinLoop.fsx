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

// Change Config nr-of-instances
let config = ConfigurationFactory.ParseString(@"
                    akka {  
                        actor {
                            deployment {
                                /localactor {
                                    router = round-robin-pool
                                    nr-of-instances = 1
                                }
                            }
                        }
                    }")

let system = System.create "system1" <| config //Configuration.load()

let local = spawnOpt system "localactor" (fun mailbox ->
        let address = mailbox.Self.Path.ToStringWithAddress()   
        let rec loop() = actor {
            let! msg = mailbox.Receive()
            Console.WriteLine("{0} got {1} - Thread #id {2}", address, msg, System.Threading.Thread.CurrentThread.ManagedThreadId)
            return! loop()
            }
        loop()) [ (SpawnOption.Router(new FromConfig())) ]


//these messages should reach the workers via the routed local ref
for i in [0..99] do
    local <! (sprintf "Local message %d" i)

system.Shutdown()


