using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Shared;

namespace Client
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
                            hostname = client
                            port = 8082
                        }
                    }

                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                    log-config-on-start = on        

                    actor {      
                    creation-timeout = 20s  
                        debug {  
                        receive = on 
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
            using (var actorSystem = ActorSystem.Create("client",  ConfigurationFactory.ParseString(config)))
            {
                var testActor = actorSystem.ActorSelection("akka.tcp://host@host:8081/user/TestActor");

                Console.WriteLine($"Sending message...");

                testActor.Ask(new TestMessage($"Message")).Wait();

                Console.WriteLine($"Message ACKed.");
            }
        }
    }
}
