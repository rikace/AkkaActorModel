#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
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

//let configGreeing = Configuration.parse  
//                     @"akka {
//            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
//            remote.helios.tcp {
//                hostname = localhost
//                port = 8088
//            }
//        }"
//
//type Greet(who:string) =
//    member x.Who = who
// 
//type GreetingActor() as g =
//    inherit ReceiveActor()
//    do g.Receive<Greet>(fun (greet:Greet) -> 
//            printfn "Hello %s" greet.Who)
//



[<EntryPoint>]
let main argv = 
    use remoteSystem = System.create "remote-system" config

    printfn "Remote Actor %s listening..." remoteSystem.Name

//    use greetingServer =  System.create "greeting-system" configGreeing
//    let greetingActor = remoteSystem.ActorOf<GreetingActor>("greeter")
     

    System.Console.ReadLine() |> ignore

    remoteSystem.Shutdown()

    0 // return an integer exit code
