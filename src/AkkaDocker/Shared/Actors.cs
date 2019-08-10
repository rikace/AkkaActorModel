using System;
using System.Net.Sockets;
using Akka;
using Akka.Actor;

namespace Shared
{
    public class TestActor : ReceiveActor
    {
        public TestActor()
        {
            Receive<TestMessage>(msg =>
            {
                Console.WriteLine(msg.Message);
                
            });
        }
    }
}
