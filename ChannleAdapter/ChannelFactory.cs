using LoggingProvider.Generic;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Configuration;

namespace MessageBroker.Messaging
{
    public abstract class ChannelFactory
    {
        protected readonly MessagingFactory _factory;
        protected readonly ILogger _logger;

        protected ChannelFactory(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(ChannelFactory) + nameof(logger));

            try
            {
                _factory = MessagingFactory.CreateFromConnectionString(connectionString);
            }
            catch (ConfigurationException e)
            {
                logger.LogError($"Failed to create a messaging channel {nameof(ChannelFactory)}. " +
                                                 $"Probably due to invalid {nameof(connectionString)}. " +
                                                 $"Exception: {e.InnerException}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to create {nameof(ChannelFactory)}: {e.GetBaseException().Message}");
                throw;
            }
        }
    }
}

