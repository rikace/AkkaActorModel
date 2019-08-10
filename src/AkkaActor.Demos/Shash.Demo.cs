namespace AkkaActor.Demos
{
    using System;
    using Akka.Actor;

    public class VariableConsoleWriterActor : ReceiveActor, IWithUnboundedStash
    {
        public VariableConsoleWriterActor()
        {
            Become(NormalWriter);
        }
        public void NormalWriter()
        {
            Receive<WriteSomethingMessage>(message =>
            {
                if (message.ThingToWrite.ToLowerInvariant() == "be quiet")
                {
                    Console.WriteLine("zipping my lips!");
                    Become(QuietWriter);
                }
                Console.WriteLine("[{0}]: {1}", Sender.Path, message.ThingToWrite);
            });
        }

        public void QuietWriter()
        {
            Receive<WriteSomethingMessage>(msg =>
            {
                if (msg.ThingToWrite.ToLowerInvariant() == "be loud")
                {
                    Console.WriteLine("Letting it all out!");
                    Stash.UnstashAll();
                    Become(NormalWriter);
                }
                else
                {
                    Stash.Stash();
                }
            });
        }

        public IStash Stash { get; set; }
    }
    
    public class WriteSomethingMessage
    {
        public string ThingToWrite { get; }

        public WriteSomethingMessage(string thingToWrite)
        {
            ThingToWrite = thingToWrite;
        }
    }

    
    public static class StashTest
    {
        public static void Run()
        {
            var actorSystem = ActorSystem.Create("becomeUnbecomeDemo");
            var variableWriter = actorSystem.ActorOf<VariableConsoleWriterActor>();

            Console.WriteLine("Say some things!");
            while (true)
            {
                var text = Console.ReadLine();
                variableWriter.Tell(new WriteSomethingMessage(text), ActorRefs.Nobody);
            }
        }
    }
}
