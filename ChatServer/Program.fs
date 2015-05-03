open System
open System.Linq
open Akka.FSharp
open Akka.Remote
open Akka.Routing
open ChatMessages
open System

[<EntryPoint>]
let main argv = 

    Console.Title <- (sprintf "Chat Server : %d" (System.Diagnostics.Process.GetCurrentProcess().Id))

    let config = 
            Configuration.parse """
                akka {  
                    log-config-on-start = on
                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                    actor {
                        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
                    }
                    remote {
                        helios.tcp {
                            transport-class = "Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote"
                            applied-adapters = []
                            transport-protocol = tcp
                            port = 8081
                            hostname = localhost
                        }
                    }
                }
                """
                
    let system = System.create "MyServer" config

    let chatServerActor =
        spawn system "ChatServer" <| fun mailbox ->
            let rec loop (clients:Akka.Actor.IActorRef list) = actor {
              
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

    Console.ReadLine() |> ignore

    system.Shutdown()



    0 // return an integer exit code


