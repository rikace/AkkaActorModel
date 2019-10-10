using System;
using Akka.Actor;

namespace ExactlyOnceDelivery.Common
{
    using DeliveryId = Int64;

    public interface IGatewayCommand { }
    public interface IGatewayEvent { }

    /// <summary>
    /// Initial message send to <see cref="GatewayEntry{T}"/> to request confirmed delivery.
    /// </summary>
    public sealed class DeliverRequest<T>
    {
        public ActorPath Recipient { get; }
        public T Payload { get; }

        public DeliverRequest(ActorPath recipient, T payload)
        {
            Recipient = recipient;
            Payload = payload;
        }
    }

    /// <summary>
    /// Send back from <see cref="GatewayExit{T}"/> to <see cref="GatewayEntry{T}"/> to confirm, 
    /// that message has been processed correctly.
    /// </summary>
    public sealed class Confirm : IGatewayCommand
    {
        public Confirm(long deliveryId)
        {
            DeliveryId = deliveryId;
        }

        public DeliveryId DeliveryId { get; }
    }

    /// <summary>
    /// An envelope sent by <see cref="GatewayEntry{T}"/> to <see cref="GatewayExit{T}"/>.
    /// It wraps the <see cref="Payload"/> that is expected to be delivered in at-least-once 
    /// deduplicated manner. With every redelivery a new <see cref="Deliver{T}"/> message is 
    /// being send with the next <see cref="DeliveryId"/> on it.
    /// </summary>
    public sealed class Deliver<T> : IGatewayCommand
    {
        public DeliveryId DeliveryId { get; }
        public T Payload { get; }

        public Deliver(DeliveryId deliveryId, T payload)
        {
            DeliveryId = deliveryId;
            Payload = payload;
        }
    }

    /// <summary>
    /// Event stored in persisent backend. It's used to acknowledge <see cref="GatewayEntry{T}"/>
    /// that is received <see cref="DeliverRequest{T}"/> and it's expected to start sending 
    /// a message to the recipient (which must inherit from <see cref="GatewayExit{T}"/>).
    /// </summary>
    public sealed class DeliverySent<T> : IGatewayEvent
    {
        public ActorPath Recipient { get; }
        public T Payload { get; }

        public DeliverySent(ActorPath recipient, T payload)
        {
            Recipient = recipient;
            Payload = payload;
        }
    }

    /// <summary>
    /// Event stored in persistent backend. It's used by the <see cref="GatewayExit{T}"/> to
    /// acknowledge that it recevied <see cref="Deliver"/> message. Used to establish deduplicates.
    /// </summary>
    public sealed class DeliveryReceived : IGatewayEvent
    {
        public DeliveryId DeliveryId { get; }

        public DeliveryReceived(long deliveryId)
        {
            DeliveryId = deliveryId;
        }
    }
    
    /// <summary>
    /// Event stored in persistent backend. It's used to acknowledge <see cref="GatewayEntry{T}"/>
    /// that a redelivery process, stared by <see cref="DeliverySent"/>, can be finished.
    /// </summary>
    public sealed class DeliveryConfirmed : IGatewayEvent
    {
        public DeliveryConfirmed(long deliveryId)
        {
            DeliveryId = deliveryId;
        }

        public DeliveryId DeliveryId { get; }
    } 
}