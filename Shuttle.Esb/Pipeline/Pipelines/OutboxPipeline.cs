﻿using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb
{
    public class OutboxPipeline : Pipeline
    {
        public OutboxPipeline(IServiceBusConfiguration configuration, GetWorkMessageObserver getWorkMessageObserver, DeserializeTransportMessageObserver deserializeTransportMessageObserver,
            DeferTransportMessageObserver deferTransportMessageObserver, SendOutboxMessageObserver sendOutboxMessageObserver,
            AcknowledgeMessageObserver acknowledgeMessageObserver, OutboxExceptionObserver outboxExceptionObserver)
        {
            Guard.AgainstNull(configuration, "configuration");

            State.SetWorkQueue(configuration.Outbox.WorkQueue);
            State.SetErrorQueue(configuration.Outbox.ErrorQueue);

            State.SetDurationToIgnoreOnFailure(configuration.Outbox.DurationToIgnoreOnFailure);
            State.SetMaximumFailureCount(configuration.Outbox.MaximumFailureCount);

            RegisterStage("Read")
                .WithEvent<OnGetMessage>()
                .WithEvent<OnAfterGetMessage>()
                .WithEvent<OnDeserializeTransportMessage>()
                .WithEvent<OnAfterDeserializeTransportMessage>();

            RegisterStage("Send")
                .WithEvent<OnDispatchTransportMessage>()
                .WithEvent<OnAfterDispatchTransportMessage>()
                .WithEvent<OnAcknowledgeMessage>()
                .WithEvent<OnAfterAcknowledgeMessage>();

            RegisterObserver(getWorkMessageObserver);
            RegisterObserver(deserializeTransportMessageObserver);
            RegisterObserver(deferTransportMessageObserver);
            RegisterObserver(sendOutboxMessageObserver);

            RegisterObserver(acknowledgeMessageObserver);

            RegisterObserver(outboxExceptionObserver); // must be last
        }
    }
}