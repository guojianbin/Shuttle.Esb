using System;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb
{
	public class HandlerExceptionEventArgs : PipelineEventEventArgs
	{
		public TransportMessage TransportMessage { get; private set; }
		public object Message { get; private set; }
		public IQueue WorkQueue { get; private set; }
		public IQueue ErrorQueue { get; private set; }
		public Exception Exception { get; private set; }

		public HandlerExceptionEventArgs(IPipelineEvent pipelineEvent,
			TransportMessage transportMessage, object message, IQueue workQueue,
			IQueue errorQueue, Exception exception)
			: base(pipelineEvent)
		{
			TransportMessage = transportMessage;
			Message = message;
			WorkQueue = workQueue;
			ErrorQueue = errorQueue;
			Exception = exception;
		}
	}
}