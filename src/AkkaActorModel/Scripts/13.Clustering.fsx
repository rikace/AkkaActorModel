module Clustering

#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
#r "../../../packages/Akka.Cluster/lib/netstandard1.6/Akka.Cluster.dll"
#endif

open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open Akka.Cluster
open System
open System.IO


let config = Configuration.parse """
                                akka {  
                                  actor {
                                    provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
                                  }
                                  remote {
                                    log-remote-lifecycle-events = off
                                    helios.tcp {
                                      hostname = "127.0.0.1"
                                      port = 2551        
                                    }
                                  }
                                  cluster {
                                    roles = ["seed"]  # custom node roles
                                    seed-nodes = ["akka.tcp://cluster-system@127.0.0.1:2551"]
                                    # when node cannot be reached within 10 sec, mark is as down
                                    auto-down-unreachable-after = 10s
                                  }
                                }
                    """
                    
                    

let system = System.create "MyClient" config

let aref =  
    spawn system "listener"
    <| fun mailbox ->
        // subscribe for cluster events at actor start 
        // and usubscribe from them when actor stops
        let cluster = Cluster.Get (mailbox.Context.System)
        cluster.Subscribe (mailbox.Self, [| typeof<ClusterEvent.IMemberEvent> |])
        mailbox.Defer <| fun () -> cluster.Unsubscribe (mailbox.Self)
        printfn "Created an actor on node [%A] with roles [%s]" cluster.SelfAddress (String.Join(",", cluster.SelfRoles))
        let rec seed () = 
            actor {
                let! (msg: obj) = mailbox.Receive ()
                match msg with
                | :? ClusterEvent.IMemberEvent -> printfn "Cluster event %A" msg
                | _ -> printfn "Received: %A" msg
                return! seed () }
        seed ()

let aref =  
    spawn system "greeter"
    <| fun mailbox ->
        let cluster = Cluster.Get (mailbox.Context.System)
        cluster.Subscribe (mailbox.Self, [| typeof<ClusterEvent.MemberUp> |])
        mailbox.Defer <| fun () -> cluster.Unsubscribe (mailbox.Self)
        let rec loop () = 
            actor {
                let! (msg: obj) = mailbox.Receive ()
                match msg with
                // wait for member up message from seed
                | :? ClusterEvent.MemberUp as up when up.Member.HasRole "seed" -> 
                    let sref = select (up.Member.Address.ToString() + "/user/listener") mailbox
                    sref <! "Hello"
                | _ -> printfn "Received: %A" msg
                return! loop () }
        loop ()



