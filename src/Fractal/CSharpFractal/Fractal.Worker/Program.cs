using Akka;
using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Configuration;


namespace Fractal.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var configDebug = ConfigurationFactory.ParseString(@"
                akka {  
                    log-config-on-start = on
                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        
                        debug {  
                          receive = on 
                          autoreceive = on
                          lifecycle = on
                          event-stream = on
                          unhandled = on
                        }
                    }
                    remote {
                        dot-netty.tcp {
		                    port = 8090
		                    hostname = 127.0.0.1
                        }
                    }
                    log-remote-lifecycle-events = DEBUG
                }
                ");
            
            
            var config = ConfigurationFactory.ParseString(@"
                akka {  
                    log-config-on-start = on
                    stdout-loglevel = DEBUG
                    loglevel = ERROR

                    remote {
                        helios.tcp {
                            transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
		                    applied-adapters = []
		                    transport-protocol = tcp
		                    port = 8090
		                    hostname = localhost
                        }
                    }
                }
                ");
            Console.Title = "Remote Worker";

            using (var system = ActorSystem.Create("RemoteSystem", configDebug))
            {
                Console.ReadLine();
            }
        }
    }
}
