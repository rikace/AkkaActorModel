using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaActor.Demos
{

    using Akka.Actor;
    using Akka.Routing;

    using System;
    public class RoutingDemo
    {
        private static ActorSystem system;

        private static IActorRef spActor;

        public static void Start()
        {
            system = ActorSystem.Create("fizz-buzz");
            spActor = system.ActorOf(Props.Create<SupervisorActor>(), "sp-actor");
            spActor.Tell("start");

            Console.ReadKey();
        }

        public class FizzBuzzActor : ReceiveActor
        {
            private static Random Rand;

            protected override void PreStart()
            {
                Rand = new Random();
            }

            public FizzBuzzActor()
            {
                Receive<FizzBuzzMessage>(msg =>
                {
                    try
                    {
                        if (Rand.Next(10000) <= 20)
                        {
                            throw new ArithmeticException();
                        }

                        var i = msg.Number;

                        var isFizz = false;
                        var isBuzz = false;

                        if (i % 3 == 0) { isFizz = true; }
                        if (i % 5 == 0) { isBuzz = true; }

                        var msg2 = string.Format("{0} - {1}", Self.Path, i);
                        var msg3 = string.Empty;

                        if (isFizz && isBuzz)
                        {
                            msg2 = msg2 + " - FizzBuzz";
                            msg3 = "fizzbuzz";
                        }
                        else if (isFizz)
                        {
                            msg2 = msg2 + " - Fizz";
                            msg3 = "fizz";
                        }
                        else if (isBuzz)
                        {
                            msg2 = msg2 + " - Buzz";
                            msg3 = "buzz";
                        }
                        else
                        {
                            msg3 = "number";
                        }

                        Context.ActorSelection("/user/sp-actor/cw-actor").Tell(msg2);
                        Sender.Tell(msg3);
                    }
                    catch (Exception ex)
                    {
                        Sender.Tell("error");
                        Self.Forward(msg);
                        throw;
                    }
                });
            }
        }

        public class ConsoleWriterActor : ReceiveActor
        {
            private static int counter;

            private static bool halfway;

            protected override void PreStart()
            {
                counter = 1;
                halfway = false;
                Become(Writer);
            }

            private void Writer()
            {
                Receive<string>(msg =>
                {
                    Console.WriteLine("{0}> {1}", counter, msg);
                    counter++;

                    if (counter > 5000 && !halfway)
                    {
                        BecomeStacked(ColourSwitcher);
                    }
                });
            }

            private void ColourSwitcher()
            {
                Receive<string>(msg =>
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    halfway = true;
                    UnbecomeStacked();
                });
            }
        }

        public class SupervisorActor : ReceiveActor
        {
            private static IActorRef fbActor;

            private static IActorRef cwActor;

            private static int fizz;

            private static int buzz;

            private static int fizzBuzz;

            private static int errors;

            private static int total;

            protected override SupervisorStrategy SupervisorStrategy()
            {
                return
                    new OneForOneStrategy(100, 10, x =>
                            x is ArithmeticException
                                ? Directive.Restart
                                : Directive.Stop
                        );
            }

            protected override void PreStart()
            {
                fizz = buzz = fizzBuzz = total = 0;

                fbActor = Context.ActorOf(Props.Create<FizzBuzzActor>().WithRouter(new RoundRobinPool(100)), "fb-actor");
                cwActor = Context.ActorOf(Props.Create<ConsoleWriterActor>(), "cw-actor");
            }

            public SupervisorActor()
            {
                Receive<string>(msg => msg == "start", msg =>
                {
                    for (var i = 1; i <= 10000; i++)
                    {
                        fbActor.Tell(new FizzBuzzMessage(i));
                    }
                });


                Receive<string>(msg =>
                {
                    if (msg == "fizz")
                    {
                        fizz++;
                        total++;
                    }
                    else if (msg == "buzz")
                    {
                        buzz++;
                        total++;
                    }
                    else if (msg == "fizzbuzz")
                    {
                        fizzBuzz++;
                        total++;
                    }
                    else if (msg == "number")
                    {
                        total++;
                    }
                    else if (msg == "error")
                    {
                        errors++;
                    }

                    if (total == 10000)
                    {
                        cwActor.Tell(string.Format("{0} / {1} / {2} / {3}", fizz, buzz, fizzBuzz, errors));
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
    }
}
