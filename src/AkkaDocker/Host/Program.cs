using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Shared;

namespace AkkaDocker
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = @"akka {     
                    actor {
                        provider = remote
                    }

                    remote {
                        dot-netty.tcp {
                            enabled-transports = [""akka.remote.netty.tcp""]
                            hostname = host
                            port = 8081
                        }
                    }

                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                    log-config-on-start = on
                    actor {
                    creation-timeout = 20s
                        debug { receive = on
                        autoreceive = on
                        lifecycle = on

                    event-stream = on
                        unhandled = on

                    fsm = on
                    event-stream = on
                        log-sent-messages = on
                        log-received-messages = on
                        router-misconfiguration = on
                    }
                }
            }";
            
            using (var actorSystem = ActorSystem.Create("host", ConfigurationFactory.ParseString(config)))
            {
                var testActor = actorSystem.ActorOf(Props.Create<TestActor>(), "TestActor");
                Console.WriteLine($"Waiting for requests...");
                while (true)
                {
                    Task.Delay(1000).Wait();
                }
            }
        }
    }
}
