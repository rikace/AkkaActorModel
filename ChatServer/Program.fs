open System
open System.Linq
open Akka
open Akka
open Akka.FSharp
open Akka.Event
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open Akka.Routing
open ChatMessages

[<EntryPoint>]
let main argv = 

    Console.Title <- (sprintf "Chat Server : %d" (System.Diagnostics.Process.GetCurrentProcess().Id))
            
            //let config = ConfigurationFactory.ParseString(@"
            //akka {  
            //    log-config-on-start = on
            //    stdout-loglevel = DEBUG
            //    loglevel = ERROR
            //    actor {
            //        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            //        
            //        debug {  
            //          receive = on 
            //          autoreceive = on
            //          lifecycle = on
            //          event-stream = on
            //          unhandled = on
            //        }
            //    }
            //
            //    remote {
            //		log-received-messages = on
            //		log-sent-messages = on
            //        #log-remote-lifecycle-events = on
            //
            //        #this is the new upcoming remoting support, which enables multiple transports
            //       helios.tcp {
            //            transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
            //		    applied-adapters = []
            //		    transport-protocol = tcp
            //		    port = 8081
            //		    hostname = 0.0.0.0 #listens on ALL ips for this machine
            //            public-hostname = localhost #but only accepts connections on localhost (usually 127.0.0.1)
            //        }
            //        log-remote-lifecycle-events = INFO
            //    }
            //
            //}
            //")

    let fluentConfig = 
        FluentConfig.Begin()
                .StdOutLogLevel(LogLevel.DebugLevel)
                .LogConfigOnStart(true)
                .LogLevel(LogLevel.ErrorLevel)
                .LogLocal(true, true,true,true, true)                
                .LogRemote(LogLevel.DebugLevel, true, true)
                .StartRemotingOn("localhost", 8081)                
                .Build()


    let system = System.create "MyServer" fluentConfig

    let chatServerActor =
        spawn system "ChatServer" <| fun mailbox ->
            let rec loop (clients:ActorRef list) = actor {
                let! (msg:obj) = mailbox.Receive()
                printfn "Received %A" msg
                match msg with
                | :? SayRequest as sr ->
                        let color = Console.ForegroundColor
                        Console.ForegroundColor <- ConsoleColor.Green                        
                        Console.WriteLine("{0} said {1}", sr.Username, sr.Text)

                        let response = SayResponse(sr.Username, sr.Text)
                        for client in clients do
                            client.Tell(response, mailbox.Self)
                        
                        Console.ForegroundColor <- color
                        return! loop clients

                | :? ConnectRequest as cr ->
                    let response = ConnectResponse(cr.UserName + " Hello and welcome to Akka .NET chat example")
                    let sender = mailbox.Sender()
                    sender.Tell(response, mailbox.Self)

                    let color = Console.ForegroundColor
                    Console.ForegroundColor <- ConsoleColor.Green                        
                    Console.WriteLine("{0} has joined the chat", cr.UserName)
                    Console.ForegroundColor <- color

                    return! loop (sender :: clients)             
                | :? NickRequest as nr -> 
                        let response = NickResponse(nr.OldUsername, nr.NewUSername)
                        for client in clients do
                           client.Tell(response, mailbox.Self)
                        
                        return! loop clients

                | :? SayResponse as sr -> 
                                Console.WriteLine("{0}: {1}", sr.Username, sr.Text)
                                return! loop clients
                | _ -> ()
                }
            loop []


//    let chatServerActor =
//        spawn system "ChatServer" <| fun mailbox ->
//            let rec loop (clients:ActorRef list) = actor {
//                let! (msg:ChatMessage) = mailbox.Receive()
//                match msg with
//                | SayRequest(username, text) ->
//                        let color = Console.ForegroundColor
//                        Console.ForegroundColor <- ConsoleColor.Green                        
//                        Console.WriteLine("{0} said {1}",username, text)
//
//                        let response = SayResponse(username, text)
//                        for client in clients do
//                            client.Tell(response, mailbox.Self)
//                        
//                        Console.ForegroundColor <- color
//
//                | ConnectRequest(username) ->
//                    let response = ConnectResponse( "Hello and welcome to Akka .NET chat example")
//                    let sender = mailbox.Sender()
//                    sender.Tell(response, mailbox.Self)
//                    return! loop (sender :: clients)             
//                | NickRequest(oldUsername, newUsername) -> 
//                        let response = NickResponse(oldUsername, newUsername)
//                        for client in clients do
//                           client.Tell(response, mailbox.Self)
//                        
//                        return! loop clients
//
//
//                | SayResponse(username, text) -> 
//                                Console.WriteLine("{0}: {1}", username, text)
//                | Disconnect -> ()
//                }
//            loop []



   // system.ActorOf<chatServerActor>("ChatServer")


    Console.ReadLine() |> ignore

    system.Shutdown()



    0 // return an integer exit code
