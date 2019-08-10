using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Configuration;


namespace AkkaFractal
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {  
                    log-config-on-start = on
                    stdout-loglevel = DEBUG
                    loglevel = ERROR
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        debug {
                          receive = on
                          autoreceive = on
                          lifecycle = on
                          event-stream = on
                          unhandled = on
                        }        
                        deployment {
                            /render {
                                router = round-robin-pool
                                nr-of-instances = 16               
                            }
                        }
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
                }
                ");
            Console.Title = "Akka Fractal";

            using (ActorSystem system = ActorSystem.Create("fractal", config))
            {
                Fractal.Run(system);
                Console.ReadLine();
            }
        }
    }
}
