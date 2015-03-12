module Routing


#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.FSharp
open System
open Akka.Actor
open Akka.Routing
open Akka.Configuration
open Akka.FSharp

type Job = {Value:string}

type WorkerActor() =
    inherit TypedActor()

    interface IHandle<Job> with 
        member this.Handle(job)= 
             printfn "Working on Job : %s" job.Value


let system = ActorSystem.Create("system")

 
let myFunc = (fun (mailbox:Actor<Job>) ->
        let rec loop() =
            actor {
                let! job = mailbox.Receive()
                let sender = mailbox.Sender()
                printfn "Working on Job : %s" job.Value
                return! loop()
            }
        loop())

let worker1 = spawn system "worker1" myFunc
let worker2 = spawn system "worker2" myFunc
let worker3 = spawn system "worker3" myFunc

let routingSystem = system.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup([|worker1;worker2;worker3|])))

let value = System.Console.ReadLine()

routingSystem <! {Value=value}

worker1.Tell {Value = "Ricky"}



