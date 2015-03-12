module Remote


#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System.IO

// Actor can also be used for distributed computing

let actorConfiguration = 
    ConfigurationFactory.ParseString("""
          akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }

            remote {
                helios.tcp {
                    port = 8004
                    hostname = localhost
                }
            }
        }""")


let system = ActorSystem.Create("system-Remote", actorConfiguration)



let remoteServer = 
    spawn system "RemoteServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()
                match box message with
                | :? string -> 
                        printfn "Message receice -> %s" message
                        sender <! sprintf "Echo from remote - %s" message
                        return! loop()
                | _ ->  failwith "unknown message"
            } 
        loop()

let echoClient = system.ActorSelection(
                            "akka.tcp://system-Remote@localhost:8004/user/RemoteServer")

let task = echoClient <? "Akka.Net & F# rock!"

async {
       let! response = task
       printfn "Reply from remote %s" (string(response))
} |> Async.Start

system.Shutdown()
