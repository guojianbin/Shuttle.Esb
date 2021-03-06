﻿using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb
{
	public class DistributorMessageObserver :
		IPipelineObserver<OnHandleDistributeMessage>,
		IPipelineObserver<OnAbortPipeline>
	{
	    private readonly IWorkerAvailabilityManager _workerAvailabilityManager;

	    public DistributorMessageObserver(IWorkerAvailabilityManager workerAvailabilityManager)
	    {
            Guard.AgainstNull(workerAvailabilityManager, "workerAvailabilityManager");

	        _workerAvailabilityManager = workerAvailabilityManager;
	    }

	    public void Execute(OnHandleDistributeMessage pipelineEvent)
		{
			var state = pipelineEvent.Pipeline.State;
			var transportMessage = state.GetTransportMessage();

			transportMessage.RecipientInboxWorkQueueUri = state.GetAvailableWorker().InboxWorkQueueUri;

			state.SetTransportMessage(transportMessage);
			state.SetTransportMessageReceived(null);
		}

		public void Execute(OnAbortPipeline pipelineEvent)
		{
			var state = pipelineEvent.Pipeline.State;

			_workerAvailabilityManager.ReturnAvailableWorker(state.GetAvailableWorker());
		}
	}
}