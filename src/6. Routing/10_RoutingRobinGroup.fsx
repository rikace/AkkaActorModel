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

//let router = Akka.FSharp.SpawnOption.Router(logic)

let router = system.ActorOf(Props.Empty.WithRouter(logic))

for i in 0..200 do
    router <! "Hi"

system.Shutdown()
