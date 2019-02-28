using System.Collections.Generic;

namespace MessageBroker.Channel.Managment
{
    public interface IMessagingEntitiesBuilder
    {
        void BuildMessagingEntitiesStructure(string channelName, string subscriptionName, string sqlFilterQuery = null);
    }
}