using MessageBroker.Common;
using MessageBroker.Messaging.Inbound;

namespace MessageBroker.Inbound.Store
{
    public interface IInboundChannelStore
    {
        void AddInboundChannel(string channelName, string subscriptionName, ushort maxConcurrentReaders);
        void RemoveAllChannelsAsync();
        Maybe<IMessageListener> GetChannelSubscriber(string channelName, string subscriptionName);
    }
}