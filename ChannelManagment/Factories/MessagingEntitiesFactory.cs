using LoggingProvider.Generic;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MessageBroker.Channel.Managment
{
    public sealed class MessagingEntitiesFactory : IMessagingEntitiesFactory
    {
        private readonly ILogger _logger;
        private readonly NamespaceManager _namespaceMgr;
        private readonly string _connectionString;

        public MessagingEntitiesFactory(string ConnectionStringSeviceBusNamespace, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(MessagingEntitiesFactory) + nameof(logger));

            _connectionString = ConnectionStringSeviceBusNamespace;
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new ArgumentException("A connection string is mandatory but was not provided");
            try
            {
                _namespaceMgr = NamespaceManager.CreateFromConnectionString(_connectionString);
            }
            catch (ConfigurationException e)
            {
                _logger.LogError("Failed to create a Service Bus NamespaceMngr. " +
                                                 $"Probably due to invalid {nameof(_connectionString)}. " +
                                                 $"Exception: {e.InnerException}");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to create {nameof(MessagingEntitiesFactory)}: {e.GetBaseException().Message}");
                throw;
            }
        }

        [ExcludeFromCodeCoverage]
        public async Task DeleteChannelAsync(string channel)
        {
            if (!_namespaceMgr.TopicExists(channel)) return;
            _logger.LogInfo("Deleting channel: " + channel);
            await _namespaceMgr.DeleteTopicAsync(channel);
        }

        [ExcludeFromCodeCoverage]
        public async Task CreateChannelAsync(string channel)
        {
            if (_namespaceMgr.TopicExists(channel)) return;
            _logger.LogInfo("Creating channel: " + channel);
            var topicDescription = new TopicDescription(channel)
            {
                MaxSizeInMegabytes = 4096
            };

            await _namespaceMgr.CreateTopicAsync(topicDescription);
        }
        [ExcludeFromCodeCoverage]
        public async Task CreateOrUpdateSubscriptionAsync(string channelName, string subscriptionName, string sqlFilterQuery = null)
        {
            sqlFilterQuery = string.IsNullOrEmpty(sqlFilterQuery) ? "1=1" : sqlFilterQuery;
            var sqlFilter = new SqlFilter(sqlFilterQuery);
            if (_namespaceMgr.SubscriptionExists(channelName, subscriptionName))
            {
                //todo: add this functionality to MessageBroker.Messaging.Inbound.ChannelSubscriber instead of using MessagingFactory directly 
                var channelFactory = MessagingFactory.CreateFromConnectionString(_connectionString);
                var subscriptionClient = channelFactory.CreateSubscriptionClient(channelName, subscriptionName);
                await subscriptionClient.RemoveRuleAsync(RuleDescription.DefaultRuleName);
                await subscriptionClient.AddRuleAsync(RuleDescription.DefaultRuleName, sqlFilter);
            }
            else
            {
                _logger.LogInfo($"Creating subscription [{subscriptionName}], channel [{channelName}]");
                await _namespaceMgr.CreateSubscriptionAsync(channelName, subscriptionName, sqlFilter);
            }
        }
    }
}