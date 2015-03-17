#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif


open Akka.FSharp
open Akka.Configuration
open System

type SomeActorMessages =
    | Greet of string
    | Hi

let system = ConfigurationFactory.Default() |> System.create "FSharpActors"

let actor = 
    spawn system "MyActor"
    <| fun mailbox ->
        let rec again name =
            actor {
                let! message = mailbox.Receive()
                match message with
                | Greet(n) when n = name ->
                    printfn "Hello again, %s" name
                    return! again name
                | Greet(n) -> 
                    printfn "Hello %s" n
                    return! again n
                | Hi -> 
                    printfn "Hello from F#!"
                    return! loop() }
        and loop() =
            actor {
                let! message = mailbox.Receive()
                match message with
                | Greet(name) -> 
                    printfn "Hello %s" name
                    return! again name
                | Hi ->
                    printfn "Hello from F#!"
                    return! loop() } 
        loop()

actor <! Greet "Ricky"
actor <! Hi
actor <! Greet "Ricky"
actor <! Hi
actor <! Greet "Ryan"
actor <! Hi
actor <! Greet "Ryan"
