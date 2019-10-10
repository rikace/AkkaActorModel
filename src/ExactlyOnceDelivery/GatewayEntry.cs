using Akka;
using Akka.Persistence;

namespace ExactlyOnceDelivery.Common
{
    public class GatewayEntry<T> : AtLeastOnceDeliveryActor
    {
        // we'll use those numbers to create current state snapshot every 100 events saved
        private const int SnapshotInterval = 100;
        private int counter = 0;

        public GatewayEntry(string persistenceId)
        {
            PersistenceId = persistenceId;
        }

        public override string PersistenceId { get; }

        protected override bool ReceiveRecover(object message) => message.Match()
            // try to recover from the latests snapshot if possible
            .With<SnapshotOffer>(offer => SetDeliverySnapshot((Akka.Persistence.AtLeastOnceDeliverySnapshot)offer.Snapshot))
            .With<IGatewayEvent>(UpdateState)
            .WasHandled;

        protected override bool ReceiveCommand(object message) => message.Match()
            // send by the requestor to reliably deliver a message T to the recipient
            // send by the recipient to confirm that the message was delivered successfully
            .With<Confirm>(confirm => PersistEvent(new DeliveryConfirmed(confirm.DeliveryId)))
            // send by the snapshot store to confirm, that snapshot has been saved
            .With<SaveSnapshotSuccess>(snapshotSaved =>
            {
                var snapshotSeqNr = snapshotSaved.Metadata.SequenceNr;
                // delete all messages from journal and snapshot store before latests confirmed
                // snapshot, we won't need them anymore
                DeleteMessages(snapshotSeqNr);
                DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
            })
            .With<DeliverRequest<T>>(order => PersistEvent(new DeliverySent<T>(order.Recipient, order.Payload)))
            .WasHandled;

        private void PersistEvent(IGatewayEvent e)
        {
            // persist event
            Persist(e, UpdateState);

            // check if it's turn to save at-least-once-delivery state into snapshot store
            counter = (counter + 1) % SnapshotInterval;
            if (counter == 0)
            {
                var snapshot = GetDeliverySnapshot();
                SaveSnapshot(snapshot);
            }
        }

        private void UpdateState(IGatewayEvent message) => message.Match()
            // once message sent request has been stored, start delivery procedure to recipient
            .With<DeliverySent<T>>(sent => Deliver(sent.Recipient, deliveryId => new Deliver<T>(deliveryId, sent.Payload)))
            // once message confirmation has been stored, officially confirm that delivery
            .With<DeliveryConfirmed>(confirmed => ConfirmDelivery(confirmed.DeliveryId));
    }
}