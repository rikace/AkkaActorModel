module HelloActor

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka
open Akka.FSharp
open Akka.Actor
open System

type Hello =
    Hello of who:string

type HelloActor() =
    inherit ReceiveActor()
    
    do
        base.Receive<Hello>(new Action<Hello>(fun hello -> 
                    match hello with
                    | Hello(who) -> (printfn "Hello %s!!"who)))
                        



let system = ActorSystem.Create("system")

let helloActor = system.ActorOf<HelloActor>()

helloActor <! Hello("Ricky")

system.Shutdown()
