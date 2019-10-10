using System;
using ExactlyOnceDelivery.Common;

namespace ExactlyOnceDelivery
{
    public class Receiver : GatewayExit<string>
    {
        public Receiver(string persistenceId) : base(new RingDeduplicator(100))
        {
            PersistenceId = persistenceId;
        }

        public override string PersistenceId { get; }
        protected override void Delivered(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Received: {message}");
            Console.ResetColor();
        }
    }
    
//    static void Main(string[] args)
//    {
//        var config = ConfigurationFactory.Load()
//                .WithFallback(SqlitePersistence.DefaultConfiguration());
//
//            using (var system = ActorSystem.Create("server-system", config))
//        {
//            var receiver = system.ActorOf(Props.Create(() => new Receiver("receiver-1")), "receiver-1");
//            Console.WriteLine($"Receiver starts listening: {receiver}");
//
//            Console.ReadLine();
//        }
//    }
}