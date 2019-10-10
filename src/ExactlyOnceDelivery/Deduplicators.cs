using System;
using System.Collections.Immutable;
using System.Linq;

namespace ExactlyOnceDelivery.Common
{
    using DeliveryId = Int64;

    /// <summary>
    /// Common interface used for message deduplication.
    /// </summary>
    public interface IDeduplicator
    {
        /// <summary>
        /// Returns true if message is unique accordingly to current 
        /// deduplicator logic. False if it's a duplicate.
        /// </summary>
        /// <returns></returns>
        bool Acknowledge(DeliveryId deliveryId);
    }

    /// <summary>
    /// Deduplicator based on unique monotonicly increasing delivery nr.
    /// </summary>
    public sealed class MonotonicDeduplicator : IDeduplicator
    {
        public DeliveryId lastKnown = 0;

        public bool Acknowledge(DeliveryId deliveryId)
        {
            if (lastKnown < deliveryId)
            {
                lastKnown = deliveryId;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Deduplicator based on ring of N latest delivery nr.
    /// </summary>
    public sealed class RingDeduplicator : IDeduplicator
    {
        public sealed class RingDeduplicatorState
        {
            public int Index { get; }
            public ImmutableArray<DeliveryId> Buffer { get; }

            public RingDeduplicatorState(int index, ImmutableArray<DeliveryId> buffer)
            {
                Index = index;
                Buffer = buffer;
            }
        }

        private int idx = 0;
        private DeliveryId[] buffer;

        public RingDeduplicator(int bufferCapacity = 100)
        {
            buffer = new DeliveryId[bufferCapacity];
        }

        public bool Acknowledge(DeliveryId deliveryId)
        {
            if (!buffer.Contains(deliveryId))
            {
                buffer[idx] = deliveryId;
                idx = (idx++) % buffer.Length;
                return true;
            }

            return false;
        }
    }
}