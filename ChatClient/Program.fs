open System
open System.Linq
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Routing
open ChatMessages

[<EntryPoint>]
let main argv = 
    
    Console.Title <- (sprintf "Chat Client : %d" (System.Diagnostics.Process.GetCurrentProcess().Id))

    Console.Write("Insert your user name: ")
    let username = Console.ReadLine()

    let config = Configuration.parse """
        akka {  
            actor {
                provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
            }
            remote {
                helios.tcp {
                    transport-class = "Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote"
		            applied-adapters = []
		            transport-protocol = tcp
		            port = 0
		            hostname = localhost
                }
            }
    }"""

    
    let system = System.create "MyClient" config
   
    let chatClientActor =
        spawn system "ChatClient" <| fun mailbox ->
           
            let server = mailbox.Context.ActorSelection("akka.tcp://MyServer@localhost:8081/user/ChatServer")            
            let rec loop nick = actor {
                let! (msg:obj) = mailbox.Receive()
                match msg with
                | :? ConnectRequest as cr ->                             
                            Console.WriteLine("Connecting....")
                            server.Tell(ConnectRequest(cr.UserName))
                            return! loop cr.UserName
                      
                |  :? ConnectResponse as cr -> 
                        Console.WriteLine("Connected!")
                        Console.WriteLine(cr.Message)
                        return! loop nick
           
                | :? NickRequest as nr ->
                        Console.WriteLine("Changing nick to {0}", nr.NewUSername)                    
                        server.Tell(NickRequest(nick, nr.NewUSername))
                        return! loop nr.NewUSername
           
                | :? NickResponse as nr ->
                        Console.WriteLine("{0} is now known as {1}", nr.OldUsername, nr.NewUSername)
                        return! loop nick
           
                | :? SayResponse as sr ->                                 
                                if sr.Username <> nick then 
                                    let orginalColor = Console.ForegroundColor
                                    Console.ForegroundColor <- ConsoleColor.Green
                                    Console.WriteLine("{0}: {1}", sr.Username, sr.Text)
                                    Console.ForegroundColor <- orginalColor
                                return! loop nick
              
                | :? SayRequest as sr -> 
                                let request = SayRequest(nick, sr.Text)
                                server.Tell(request)
                                return! loop nick

                | _ -> ()
                }
            loop ""

    chatClientActor <! (ConnectRequest(username))

    while true do
        let input = Console.ReadLine()
        if input.StartsWith("/") then 
            let parts = input.Split(' ')
            let cmd = parts.[0].ToLowerInvariant()
            let rest = String.Join(" ", parts.Skip(1))
            if cmd = "/nick" then
                chatClientActor.Tell(NickRequest(username, rest))
        else           
            chatClientActor.Tell(SayRequest(username, input))


    0  