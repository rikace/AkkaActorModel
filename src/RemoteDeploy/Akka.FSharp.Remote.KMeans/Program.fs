open System
open Akka.FSharp
open System.IO
open Akka.Configuration

[<EntryPoint>]
let main argv =
    
    System.Console.Title <- "Remote Server: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()
    
    let configText = File.ReadAllText("akka.conf")
    let config = ConfigurationFactory.ParseString(configText)

    use remoteSystem = System.create "RemoteSystem" config

    printfn "Remote Actor %s listening..." remoteSystem.Name
    
    Console.ReadLine() |> ignore
    0 // return an integer exit code
