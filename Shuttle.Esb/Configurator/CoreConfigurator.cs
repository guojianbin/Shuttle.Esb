﻿using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb
{
	public class CoreConfigurator : IConfigurator
	{
		public void Apply(IServiceBusConfiguration configuration)
		{
			Guard.AgainstNull(configuration, "configuration");

            var transactionScopeElement = TransactionScopeSection.Get();

            configuration.TransactionScope = transactionScopeElement != null
                ? new TransactionScopeConfiguration
                {
                    Enabled = transactionScopeElement.Enabled,
                    IsolationLevel = transactionScopeElement.IsolationLevel,
                    TimeoutSeconds = transactionScopeElement.TimeoutSeconds
                }
                : new TransactionScopeConfiguration();

            var section = ServiceBusSection.Get();

			if (section == null)
			{
				return;
			}

			configuration.CreateQueues = section.CreateQueues;
			configuration.CacheIdentity = section.CacheIdentity;
			configuration.RegisterHandlers = section.RegisterHandlers;
			configuration.RemoveMessagesNotHandled = section.RemoveMessagesNotHandled;
			configuration.CompressionAlgorithm = section.CompressionAlgorithm;
			configuration.EncryptionAlgorithm = section.EncryptionAlgorithm;
		}
	}
}