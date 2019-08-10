using Akka.Actor;
using Akka.Configuration;
using ChatMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(@"
            akka {  
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                }
                remote {
                    helios.tcp {
                        transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
		                applied-adapters = []
		                transport-protocol = tcp
		                port = 0
		                hostname = localhost
                    }
                }
            }");

            Console.Title = $"Chat Client : {System.Diagnostics.Process.GetCurrentProcess().Id}";

            Console.Write("Insert your user name: ");
            var username = Console.ReadLine();

            using (var system = ActorSystem.Create("MyClient", config))
            {
                var chatClient = system.ActorOf(Props.Create<ChatClientActor>(username));
                system.ActorSelection("akka.tcp://MyServer@localhost:8081/user/ChatServer");
                chatClient.Tell(new ConnectRequest()
                {
                    Username = username
                });

                while (true)
                {
                    var input = Console.ReadLine();
                    if (input.StartsWith("/"))
                    {
                        var parts = input.Split(' ');
                        var cmd = parts[0].ToLowerInvariant();
                        var rest = string.Join(" ", parts.Skip(1));

                        if (cmd == "/nick")
                        {
                            chatClient.Tell(new NickRequest
                            {
                                NewUsername = rest
                            });
                        }
                    }
                    else
                    {
                        chatClient.Tell(new SayRequest()
                        {
                            Text = input,
                        });
                    }
                }
            }
        }
    }

    class ChatClientActor : TypedActor,
        IHandle<ConnectRequest>,
        IHandle<ConnectResponse>,
        IHandle<NickRequest>,
        IHandle<NickResponse>,
        IHandle<SayRequest>,
        IHandle<SayResponse>, ILogReceive
    {
        public ChatClientActor(string username)
        {
            _nick = username;
        }
        private string _nick;
        private readonly ActorSelection _server = Context.ActorSelection("akka.tcp://MyServer@localhost:8081/user/ChatServer");

        public void Handle(ConnectResponse message)
        {
            Console.WriteLine("Connected!");
            Console.WriteLine(message.Message);
        }

        public void Handle(NickRequest message)
        {
            message.OldUsername = this._nick;
            Console.WriteLine("Changing nick to {0}", message.NewUsername);
            this._nick = message.NewUsername;
            _server.Tell(message);
        }

        public void Handle(NickResponse message)
        {
            Console.WriteLine("{0} is now known as {1}", message.OldUsername, message.NewUsername);
        }

        public void Handle(SayResponse message)
        {
            ConsoleColor color = Console.ForegroundColor;
            if (message.Username == _nick)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("{0}: {1}", message.Username, message.Text);
            Console.ForegroundColor = color;
        }

        public void Handle(ConnectRequest message)
        {
            Console.WriteLine("Connecting....");
            _server.Tell(message);
        }

        public void Handle(SayRequest message)
        {
            message.Username = this._nick;
            _server.Tell(message);
        }
    }
}
