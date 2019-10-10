open System
open Akka.FSharp
open System.IO
open Akka.Actor
open Akka.Configuration
open Akka.Client.Actors

open Akka.Configuration
[<EntryPoint>]
let main argv =
    System.Console.Title <- "Client : " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()
        
    let configText = File.ReadAllText("akka.conf")
    let config = ConfigurationFactory.ParseString(configText)

    use system = System.create "local-system" config
    
    let remoteSystemAddress = "akka.tcp://RemoteSystem@0.0.0.0:8090"

//    let spawnRemote systemOrContext remoteSystemAddress actorName expr =
//        spawne systemOrContext actorName expr [SpawnOption.Deploy (Deploy(RemoteScope (Address.Parse remoteSystemAddress)))]
//        
//    let aref =
//        spawnRemote system remoteSystemAddress "hello"
//          // actorOf wraps custom handling function with message receiver logic
//          <@ actorOf (fun msg -> System.Console.ForegroundColor <- System.ConsoleColor.Yellow
//                                 printfn "received 42  '%s'" msg) @>        
//        
//    let sref = select "akka://local-system/user/hello" system  
//    sref <! "Hello again"
//        
//    Console.WriteLine("Press [ENTER] to exit.")
//    Console.ReadLine() |> ignore      
    
    
    let kmActor =
        RemoteKMeans.kmExpr remoteSystemAddress system
        
//    let irisData = RemoteKMeans.irisData |> Async.RunSynchronously
//    printfn "data : %A" irisData
//    
//    Console.WriteLine("Press [ENTER] to exit.")
//    Console.ReadLine() |> ignore
    
    
    //  kmExpr.Tell irisData
    let computation = async {
        try
            let! irisData = RemoteKMeans.irisData
            printfn "Sent Message"           
            let msg : Async<float [][][]>= kmActor <? irisData
            let! result = msg
            printfn "actor responded with data size : %d" (Seq.length result)
            printfn "actor responded with result : %A" result
        with
        | ex -> let msg = ex.Message
                do printfn "Error %s" msg
                ()
    }
    computation |> Async.RunSynchronously

    
        
    Console.WriteLine("Press [ENTER] to exit.")
    Console.ReadLine() |> ignore



    Console.ReadLine() |> ignore
    0 // return an integer exit code



