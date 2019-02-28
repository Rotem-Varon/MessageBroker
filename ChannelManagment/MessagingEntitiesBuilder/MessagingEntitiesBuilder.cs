using System;
using System.Collections.Generic;

namespace MessageBroker.Channel.Managment
{
    public sealed class MessagingEntitiesBuilder : IMessagingEntitiesBuilder
    {
        readonly IMessagingEntitiesFactory _messagingEntitiesFactory;

        public MessagingEntitiesBuilder(IMessagingEntitiesFactory messagingEntitiesFactory)
        {
            _messagingEntitiesFactory = messagingEntitiesFactory ?? throw new ArgumentNullException(nameof(messagingEntitiesFactory));
        }

        public void BuildMessagingEntitiesStructure(string channelName, string subscriptionName, string sqlFilterQuery = null)
        {
            CreateChannel(channelName);
            CreateSubscriptionForChannel(channelName, subscriptionName, sqlFilterQuery);
        }

        private void CreateChannel(string channelName)
        {
            _messagingEntitiesFactory.CreateChannelAsync(channelName);
        }

        public void DeleteChannel(string channelName)
        {
            _messagingEntitiesFactory.DeleteChannelAsync(channelName);
        }

        private async void CreateSubscriptionsForChannel(string channelName, List<string> subscriptionsNames)
        {
            foreach (var subscription in subscriptionsNames)
            {
                await _messagingEntitiesFactory.CreateOrUpdateSubscriptionAsync(channelName, subscription);
            }
        }

        private void CreateSubscriptionForChannel(string channelName, string subscriptionName, string sqlFilterQuery)
        {
            _messagingEntitiesFactory.CreateOrUpdateSubscriptionAsync(channelName, subscriptionName, sqlFilterQuery);
        }
    }
}