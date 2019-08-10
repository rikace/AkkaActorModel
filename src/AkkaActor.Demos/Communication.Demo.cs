using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaActor.Demos
{
    using System;

    using Akka.Actor;

    public class Communication
{
    private static ActorSystem system;

    private static IActorRef fbActor;

    private static IActorRef cwActor;

    public static void Start()
    {
        system = ActorSystem.Create("fizz-buzz");
        fbActor = system.ActorOf(Props.Create<FizzBuzzActor>(), "fb-actor");
        cwActor = system.ActorOf(Props.Create<ConsoleWriterActor>(), "cw-actor");

        for (var i = 1; i <= 10000; i++)
        {
            fbActor.Tell(new FizzBuzzMessage(i));
        }

        Console.ReadKey();
    }

    public class FizzBuzzActor : ReceiveActor
    {
        public FizzBuzzActor()
        {
            Receive<FizzBuzzMessage>(msg =>
            {
                var i = msg.Number;

                var isFizz = false;
                var isBuzz = false;

                if (i % 3 == 0) { isFizz = true; }
                if (i % 5 == 0) { isBuzz = true; }

                var msg2 = i.ToString();

                if (isFizz && isBuzz)
                {
                    msg2 = msg2 + " - FizzBuzz";
                }
                else if (isFizz)
                {
                    msg2 = msg2 + " - Fizz";
                }
                else if (isBuzz)
                {
                    msg2 = msg2 + " - Buzz";
                }

                cwActor.Tell(msg2);
            });
        }
    }

    public class ConsoleWriterActor : ReceiveActor
    {
        public ConsoleWriterActor()
        {
            Receive<string>(msg => Console.WriteLine(msg));
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
}
}
