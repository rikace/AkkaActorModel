module EchoActor


#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System

// An Actor is more lightweight than a thread. Millions of actors can be generated in Akka,
// the secret is that an Actor can reuse a thread.

let system = ActorSystem.Create("Echo-System")

type EchoActor(name) =
    inherit Actor()

    override x.OnReceive message =
        let tid = Threading.Thread.CurrentThread.ManagedThreadId
        match message with
        | :? string as msg -> printfn "Hello %s from %s at #%d thread" msg name tid
        | _ ->  failwith "unknown message"

let echoServers = 
    [1 .. 1000]
    |> List.map(fun id ->   let properties = [| string(id) :> obj |]
                            system.ActorOf(Props(typedefof<EchoActor>, properties)))

let rand = Random(1234)

for id in [1 .. 1000] do
    (rand.Next() % 10) |> List.nth echoServers <! sprintf "F# request %d!" id

system.Shutdown()

