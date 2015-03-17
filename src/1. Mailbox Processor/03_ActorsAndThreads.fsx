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


// #Actor Implementation
// An Actor is more lightweight than a thread. Millions of actors can be generated in Akka,
// the secret is that an Actor can reuse a thread.
//
// The mapping relationship between an Actor and a Thread is decided by a Dispatcher.
// 
// This example creates 10 Actors, and prints its thread name when invoked.
//
// You will find there is no fixed mapping relationship between Actors and Threads. 
// An Actor can use many threads. And a thread can be used by many Actors.

let system = ActorSystem.Create("FSharp")

type EchoServer(name) =
    inherit Actor()

    override x.OnReceive message =
        let tid = Threading.Thread.CurrentThread.ManagedThreadId
        match message with
        | :? string as msg -> printfn "Hello %s from %s at #%d thread" msg name tid
        | _ ->  failwith "unknown message"


        // START TASK MANAGER
let count = 20000
let echoServers = 
    [1 .. count]
    |> List.map(fun id ->   let properties = [| string(id) :> obj |]
                            system.ActorOf(Props(typedefof<EchoServer>, properties)))

let rand = Random(1234)

for id in [1 .. count] do
    (rand.Next() % 10) |> List.nth echoServers <! sprintf "F# request %d!" id

system.Shutdown()