using MessageBroker.Common;
using MessageBroker.Messaging.Outbound;

namespace MessageBroker.Outbound.Store
{
    public interface IOutboundChannelStore
    {
        void AddOutboundChannel(string channelName);
        void RemoveAllChannelsAsync();
        Maybe<IMessageSender> GetOutboundChannel(string channelName);
    }
}