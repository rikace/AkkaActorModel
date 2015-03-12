#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif


open Akka.FSharp
open Akka.Configuration
open System

// define the messages which can be used to change the state,
// using a Discriminated Union
type ClimateMessage =
    | HeatUp
    | CoolDown
    | Normal
    | GetStatus of string


let system = ConfigurationFactory.Default() |> System.create "FSharpActors"

let actor = 
    spawn system "MyActor"
    <| fun mailbox ->
        let rec heat =
            actor {
                let! message = mailbox.Receive()
                
                match message with
                | CoolDown -> 
                    printfn "Cooling down"
                    return! cool()
                | HeatUp ->
                    printfn "It is already hot!!"
                    return! heat
                | GetStatus(s) ->
                    printfn "Hey %s - It hot!!" s
                    return! heat
                | Normal ->
                    printfn "Going to normal temperature"
                    return! normal() }
        and cool() =
            actor {
                let! message = mailbox.Receive()
                
                match message with
                | CoolDown -> 
                    printfn "It is already cold!"
                    return! cool()
                | HeatUp ->
                    printfn "Heating up!"
                    return! heat
                | GetStatus(s) ->
                    printfn "Hey %s - It cold!!" s
                    return! cool()
                | Normal ->
                    printfn "Going to normal temperature"
                    return! normal() }
        and normal() =
            actor {
                let! message = mailbox.Receive()
                
                match message with
                | CoolDown -> 
                    printfn "Cooling down"
                    return! cool()
                | HeatUp ->
                    printfn "Heating up!"
                    return! heat
                | GetStatus(s) ->
                    printfn "Hey %s - It is nice!!" s
                    return! heat
                | Normal ->
                    printfn "It is already room-temperature"
                    return! normal() }
        normal()

actor <! HeatUp
actor <! GetStatus("Ricky")
actor <! HeatUp
actor <! CoolDown
actor <! GetStatus("Ricky")
actor <! CoolDown
actor <! Normal
actor <! GetStatus("Ricky")

