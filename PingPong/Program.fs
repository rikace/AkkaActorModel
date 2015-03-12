#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System


type Message() =
    override x.ToString() =
        "message"


type Run() =
    override x.ToString() =
        "run"


type Started() =
    override x.ToString() =
        "started"



[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    0 // return an integer exit code
