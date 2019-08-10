using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaActor.Demos
{
    class ReceiveActorDemo
    {
        public static void Start()
        {
            var system = ActorSystem.Create("fizz-buzz");
            var actor = system.ActorOf(Props.Create<FizzBuzzActor>(), "fb-actor");

            for (var i = 1; i <= 10000; i++)
            {
                actor.Tell(new FizzBuzzMessage(i), actor);
            }

            Console.ReadKey();
        }

        public class FizzBuzzActor : ReceiveActor
        {
            public FizzBuzzActor()
            {
                Receive<ConsoleMessage>(msg =>
                {
                    ConsoleColor color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg.Message);
                    Console.ForegroundColor = color;
                });

                Receive<FizzBuzzMessage>(msg =>
                 {

                     var i = msg.Number;

                     var isFizz = false;
                     var isBuzz = false;

                     if (i % 3 == 0) { isFizz = true; }
                     if (i % 5 == 0) { isBuzz = true; }

                     Console.Write(i);

                     var sender = Context.Sender;
                     if (isFizz && isBuzz)
                     {
                         sender.Tell(new ConsoleMessage(" - FizzBuzz"));
                        //Console.WriteLine(" - FizzBuzz");
                    }
                     else if (isFizz)
                     {
                         sender.Tell(new ConsoleMessage(" - Fizz"));
                        //Console.WriteLine(" - Fizz");
                    }
                     else if (isBuzz)
                     {
                         sender.Tell(new ConsoleMessage(" - Buzz"));
                        //Console.WriteLine(" - Buzz");
                    }
                     else
                     {
                         Console.WriteLine();
                     }
                 });
            }
        }

        public class FizzBuzzMessage
        {
            public FizzBuzzMessage(int number)
            {
                Number = number;
            }

            public readonly int Number;
        }

        public class ConsoleMessage
        {
            public ConsoleMessage(string msg)
            {
                Message = msg;
            }

            public readonly string Message;
        }
    }
}