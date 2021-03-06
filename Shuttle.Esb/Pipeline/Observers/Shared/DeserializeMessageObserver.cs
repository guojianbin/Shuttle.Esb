﻿using System;
using System.IO;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb
{
    public class DeserializeMessageObserver : IPipelineObserver<OnDeserializeMessage>
    {
        private readonly ISerializer _serializer;
        private readonly IServiceBusEvents _events;

        public DeserializeMessageObserver(IServiceBusEvents events, ISerializer serializer)
        {
            Guard.AgainstNull(events, "events");
            Guard.AgainstNull(serializer, "serializer");

            _events = events;
            _serializer = serializer;
        }

        public void Execute(OnDeserializeMessage pipelineEvent)
        {
            var state = pipelineEvent.Pipeline.State;

            Guard.AgainstNull(state.GetTransportMessage(), "transportMessage");
            Guard.AgainstNull(state.GetWorkQueue(), "workQueue");
            Guard.AgainstNull(state.GetErrorQueue(), "errorQueue");

            var transportMessage = state.GetTransportMessage();

            object message;

            try
            {
                using (var stream = new MemoryStream(transportMessage.Message))
                {
                    message = _serializer.Deserialize(Type.GetType(transportMessage.AssemblyQualifiedName, true, true), stream);
                }
            }
            catch (Exception ex)
            {
                transportMessage.RegisterFailure(ex.AllMessages(), new TimeSpan());

                state.GetErrorQueue().Enqueue(transportMessage, _serializer.Serialize(transportMessage));
                state.GetWorkQueue().Acknowledge(state.GetReceivedMessage().AcknowledgementToken);

                state.SetTransactionComplete();
                pipelineEvent.Pipeline.Abort();

                _events.OnMessageDeserializationException(this,
                    new DeserializationExceptionEventArgs(
                        pipelineEvent,
                        state.GetWorkQueue(),
                        state.GetErrorQueue(),
                        ex));

                return;
            }

            state.SetMessage(message);
        }
    }
}