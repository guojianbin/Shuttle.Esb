using System;
using System.Collections.Generic;
using System.Diagnostics;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb
{
    public class ServiceBus : IServiceBus
    {
        private readonly ILog _log;

        private readonly IMessageSender _messageSender;

        private IProcessorThreadPool _controlThreadPool;
        private DateTime? _dateStarted;
        private DateTime? _dateStopped;
        private IProcessorThreadPool _deferredMessageThreadPool;
        private IProcessorThreadPool _inboxThreadPool;
        private IProcessorThreadPool _outboxThreadPool;

        public ServiceBus(IServiceBusConfiguration configuration)
        {
            Guard.AgainstNull(configuration, "configuration");

            Configuration = configuration;

            Events = new ServiceBusEvents();

            _messageSender = new MessageSender(this);

            _log = Log.For(this);
        }

        public IServiceBusConfiguration Configuration { get; }
        public IServiceBusEvents Events { get; }

        public IServiceBus Start()
        {
            if (Started)
            {
                throw new ApplicationException(EsbResources.ServiceBusInstanceAlreadyStarted);
            }

            _log.Information("[starting]");

            GuardAgainstInvalidConfiguration();

            // cannot be in startup pipeline as some modules may need to observe the startup pipeline
            foreach (var module in Configuration.Modules)
            {
                module.Initialize(this);
            }

            var startupPipeline = new StartupPipeline(this);

            Events.OnPipelineCreated(this, new PipelineEventArgs(startupPipeline));

            startupPipeline.Execute();

            _inboxThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("InboxThreadPool");
            _controlThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("ControlInboxThreadPool");
            _outboxThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("OutboxThreadPool");
            _deferredMessageThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("DeferredMessageThreadPool");

            Started = true;

            _dateStarted = DateTime.Now;

            _log.Information("[started]");

            return this;
        }

        public void Stop()
        {
            if (!Started)
            {
                return;
            }

            _log.Information("[stopping]");

            var frames = new StackTrace().GetFrames();

            if (frames != null)
            {
                foreach (var frame in frames)
                {
                    _log.Information(string.Format("[frame] : {0}", frame));
                }
            }

            Configuration.Modules.AttemptDispose();

            if (Configuration.HasInbox)
            {
                if (Configuration.Inbox.HasDeferredQueue)
                {
                    _deferredMessageThreadPool.Dispose();
                }

                _inboxThreadPool.Dispose();
            }

            if (Configuration.HasControlInbox)
            {
                _controlThreadPool.Dispose();
            }

            if (Configuration.HasOutbox)
            {
                _outboxThreadPool.Dispose();
            }

            Configuration.QueueManager.AttemptDispose();

            _log.Information("[stopped]");

            _dateStopped = DateTime.Now;

            Started = false;
        }

        public bool Started { get; private set; }

        public void Dispose()
        {
            Stop();
        }

        public TransportMessage CreateTransportMessage(object message, Action<TransportMessageConfigurator> configure)
        {
            GuardStarted();

            return _messageSender.CreateTransportMessage(message, configure);
        }

        public void Dispatch(TransportMessage transportMessage)
        {
            GuardStarted();

            _messageSender.Dispatch(transportMessage);
        }

        public TransportMessage Send(object message)
        {
            GuardStarted();

            return _messageSender.Send(message);
        }

        public TransportMessage Send(object message, Action<TransportMessageConfigurator> configure)
        {
            GuardStarted();

            return _messageSender.Send(message, configure);
        }

        public IEnumerable<TransportMessage> Publish(object message)
        {
            GuardStarted();

            return _messageSender.Publish(message);
        }

        public IEnumerable<TransportMessage> Publish(object message, Action<TransportMessageConfigurator> configure)
        {
            GuardStarted();

            return _messageSender.Publish(message, configure);
        }

        private void GuardAgainstInvalidConfiguration()
        {
            Guard.Against<EsbConfigurationException>(Configuration.Serializer == null,
                EsbResources.NoSerializerException);

            Guard.Against<EsbConfigurationException>(Configuration.MessageHandlerFactory == null,
                EsbResources.NoMessageHandlerFactoryException);

            Guard.Against<WorkerException>(Configuration.IsWorker && !Configuration.HasInbox,
                EsbResources.WorkerRequiresInboxException);

            if (Configuration.HasInbox)
            {
                Guard.Against<EsbConfigurationException>(Configuration.Inbox.WorkQueue == null,
                    string.Format(EsbResources.RequiredQueueMissing, "Inbox.WorkQueue"));

                Guard.Against<EsbConfigurationException>(Configuration.Inbox.ErrorQueue == null,
                    string.Format(EsbResources.RequiredQueueMissing, "Inbox.ErrorQueue"));
            }

            if (Configuration.HasOutbox)
            {
                Guard.Against<EsbConfigurationException>(Configuration.Outbox.WorkQueue == null,
                    string.Format(EsbResources.RequiredQueueMissing, "Outbox.WorkQueue"));

                Guard.Against<EsbConfigurationException>(Configuration.Outbox.ErrorQueue == null,
                    string.Format(EsbResources.RequiredQueueMissing, "Outbox.ErrorQueue"));
            }

            if (Configuration.HasControlInbox)
            {
                Guard.Against<EsbConfigurationException>(Configuration.ControlInbox.WorkQueue == null,
                    string.Format(EsbResources.RequiredQueueMissing, "ControlInbox.WorkQueue"));

                Guard.Against<EsbConfigurationException>(Configuration.ControlInbox.ErrorQueue == null,
                    string.Format(EsbResources.RequiredQueueMissing, "ControlInbox.ErrorQueue"));
            }
        }

        public static IServiceBus Create()
        {
            return Create(null);
        }

        public static IServiceBus Create(Action<DefaultConfigurator> configure)
        {
            var configurator = new DefaultConfigurator();

            if (configure != null)
            {
                configure.Invoke(configurator);
            }

            return new ServiceBus(configurator.Configuration());
        }

        private void GuardStarted()
        {
            if (Started)
            {
                return;
            }

            throw new InvalidOperationException(
                string.Format("The ServiceBus has not been started.  Is started '{0}' and stopped '{1}'.",
                    _dateStarted.HasValue ? _dateStarted.Value.ToString("O") : "(never)",
                    _dateStopped.HasValue ? _dateStopped.Value.ToString("O") : "(never)"));
        }
    }
}