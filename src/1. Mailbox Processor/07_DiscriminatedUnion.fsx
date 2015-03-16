module DiscriminatedUnionMessage


#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#endif

open System
open Akka.FSharp
open Akka.Actor


type Message =
    | Text of string
    | Number of int
    | Unknown

let system = System.create "ds-system" <| Configuration.load()

let dsServer = 
    spawn system "dsServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                match message with
                | Text str -> printfn "Hello text %s" str
                | Number i -> printfn "Hello number %i" i
                | _ -> printfn "I have no idea!"
                return! loop()
            } 
        loop()
        
dsServer <! Text "F# is cool!"
dsServer <! Number 42

system.Shutdown()

