using ExactlyOnceDelivery.Common;

namespace ExactlyOnceDelivery
{
    public class Sender : GatewayEntry<string>
    {
        public Sender(string persistenceId) : base(persistenceId)
        {

        }
    }
}

//static void Main(string[] args)
//{
//    var config = ConfigurationFactory.Load()
//            .WithFallback(SqlitePersistence.DefaultConfiguration());
//
//        using (var system = ActorSystem.Create("client-system", config))
//    {
//        var entry = system.ActorOf(Props.Create(() => new Sender("sender-1")), "sender-1");
//        var receiver = ActorPath.Parse("akka.tcp://server-system@localhost:9001/user/receiver-1");
//
//        var i = 1;
//        var interval = TimeSpan.FromSeconds(5);
//        system.Scheduler.Advanced.ScheduleRepeatedly(interval, interval, () =>
//        {
//            entry.Deliver(receiver, "hello world" + i);
//            i++;
//        });
//
//        Console.ReadLine();
//    }
//}