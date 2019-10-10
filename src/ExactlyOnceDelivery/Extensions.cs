using System.Threading.Tasks;
using Akka.Actor;

namespace ExactlyOnceDelivery.Common
{
    public static class Extensions
    {
        public static void Deliver<T>(this IActorRef gatewayEntry, ActorPath gatewayExit, T value)
        {
            gatewayEntry.Tell(new DeliverRequest<T>(gatewayExit, value));
        }
    }
}