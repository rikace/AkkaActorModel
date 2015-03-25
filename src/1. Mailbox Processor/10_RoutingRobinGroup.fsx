#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Routing
open Akka.FSharp.System

let system = ActorSystem.Create "example6"

let fn = fun (mailbox:Actor<string>) ->
                let address = mailbox.Self.Path.ToStringWithAddress()
                let rec loop () =
                    actor {
                        let! msg = mailbox.Receive ()
                        let id = System.Threading.Thread.CurrentThread.ManagedThreadId
                        printfn "Message: %s\nAddress:%s \tId:%d" msg address id
                        return! loop ()
                    }
                loop ()

let actor1 = spawn system "actor1" fn
let actor2 = spawn system "actor2" fn

let logic = Akka.Routing.RoundRobinGroup("/user/actor1", "/user/actor2")
let router = system.ActorOf(Props.Empty.WithRouter(logic))

for i in 0..199 do
    router <! "Hi"




let worker1 = spawn system "worker1" fn
let worker2 = spawn system "worker2" fn
let worker3 = spawn system "worker3" fn

let routingSystem = system.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup([|worker1;worker2;worker3|])))

for i in 0..199 do
    routingSystem <! "Hi"
