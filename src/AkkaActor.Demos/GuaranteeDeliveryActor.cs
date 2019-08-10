namespace AkkaActor.Demos
{
 
    using Akka.Actor;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    
    public class Ack
    {
        private static readonly Ack _instance = new Ack();
        public static Ack Instance { get { return _instance; } }
    }

    public class GuaranteedDeliveryActor : ReceiveActor
    {
        readonly ActorSelection _target;
        readonly object _message;
        readonly int _maxRetries;
        int _retryCount;
        readonly TimeSpan _messageTimeout;
        public GuaranteedDeliveryActor(ActorSelection target,
            object message,
            int maxRetries,
            TimeSpan messageTimeout)
        {
            _target = target;
            _message = message;
            _maxRetries = maxRetries;
            _messageTimeout = messageTimeout;

            Receive<ReceiveTimeout>(_ =>
            {
                if (_retryCount >= _maxRetries)
                    throw new TimeoutException("Unable to deliver the message to the target in the specified number of retries");
                else
                {
                    _target.Tell(_message);
                    _retryCount++;
                }
            });

            Receive<Ack>(_ =>
            {
                SetReceiveTimeout(null);
                Context.Stop(Self);
            });
        }

        protected override void PreStart()
        {
            SetReceiveTimeout(_messageTimeout);
            _target.Tell(_message);
        }
    }
}
