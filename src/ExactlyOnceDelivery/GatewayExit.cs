using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace ExactlyOnceDelivery.Common
{
    public abstract class GatewayExit<T> : PersistentActor
    {
        //protected ILoggingAdapter Log { get; }
        public override string PersistenceId { get; }
        public readonly IDeduplicator Dedupe;
        
        protected override bool ReceiveRecover(object message)
        {
            var received = message as DeliveryReceived;
            if (received != null)
            {
                Dedupe.Acknowledge(received.DeliveryId);
                return true;
            }

            return false;
        }

        protected override bool ReceiveCommand(object message)
        {
            var deliver = message as Deliver<T>;
            if (deliver != null)
            {
                var deliveryId = deliver.DeliveryId;
                Persist(new DeliveryReceived(deliveryId), received =>
                {
                    if (Dedupe.Acknowledge(deliveryId))
                    {
                        Delivered(deliver.Payload);
                    }
                    else
                    {
                        Log.Debug("Duplicate found and ignored - deliveryId: {0}", deliveryId);
                    }

                    Sender.Tell(new Confirm(deliveryId));
                });
                return true;
            }

            return true;
        }

        protected abstract void Delivered(T message);

        protected GatewayExit(IDeduplicator deduplicator)
        {
            Log = Context.GetLogger();
            Dedupe = deduplicator;
        }
    }
}