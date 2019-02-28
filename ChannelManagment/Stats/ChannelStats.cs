using LoggingProvider.Generic;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Channel.Managment
{
    // todo: this code will not be needed once we have control bus / app insights
    public class
        ChannelStats : IChannelStats
    {
        private readonly NamespaceManager _namespaceMgr;

        public ChannelStats(string connectionStringSeviceBusNamespace, ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(MessagingEntitiesFactory) + nameof(logger));

            if (string.IsNullOrWhiteSpace(connectionStringSeviceBusNamespace))
                throw new ArgumentException("A connection string is mandatory but was not provided");

            try
            {
                _namespaceMgr = NamespaceManager.CreateFromConnectionString(connectionStringSeviceBusNamespace);
            }
            catch (ConfigurationException e)
            {
                logger.LogError("Failed to create a Service Bus NamespaceMngr. " +
                                                 $"Probably due to invalid {nameof(connectionStringSeviceBusNamespace)}. " +
                                                 $"Exception: {e.InnerException}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to create {nameof(MessagingEntitiesFactory)}: {e.GetBaseException().Message}");
                throw;
            }
        }

        public async Task<string> GetMessageCountForChannelPerSubscriptionAsync(string channelName)
        {
            StringBuilder messageCount = new StringBuilder();
            foreach (SubscriptionDescription subDesc in
                await _namespaceMgr.GetSubscriptionsAsync(channelName))
            {
                messageCount.Append($"[{channelName}:{subDesc.Name}:{subDesc.MessageCount}] ");
            }
            return messageCount.ToString();
        }

        public async Task<long> GetMessageCountForSubscriptionAsync(string channelName, string subName)
        {
            var sub = await _namespaceMgr.GetSubscriptionAsync(channelName, subName);
            return sub.MessageCount;
        }

        public async Task<string> GetMessageCountForAllChannelsPerSubscription(IList<string> channels)
        {
            StringBuilder messageCount = new StringBuilder();
            foreach (var channel in channels)
            {
                messageCount.AppendLine(await GetMessageCountForChannelPerSubscriptionAsync(channel));
            }
            return messageCount.ToString();
        }
    }
}