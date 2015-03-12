open Akka
open Akka.FSharp
open Akka.Actor
open System

[<EntryPoint>]
let main argv = 
    

      let config = 
        FluentConfig.Begin()                
                .StartRemotingOn("127.0.0.1", 8090)
                .Build()

      let system = ActorSystem.Create("worker", config)
            
      Console.ReadLine() |> ignore
      system.Shutdown()
      0
