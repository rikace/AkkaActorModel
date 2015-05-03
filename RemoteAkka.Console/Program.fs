#if INTERACTIVE
#r @"..\bin\Akka.dll"
#r @"..\bin\Akka.FSharp.dll"
#r @"..\bin\Akka.Remote.dll"
#r @"..\bin\FSharp.PowerPack.dll"
#endif

open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System


let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 9234
            }
        }"


[<EntryPoint>]
let main argv = 
    
    // NO REFERENCE

    use remoteSystem = System.create "remote-system" config

    printfn "Remote Actor %s listening..." remoteSystem.Name

    System.Console.ReadLine() |> ignore

    remoteSystem.Shutdown()

    0 // return an integer exit code
