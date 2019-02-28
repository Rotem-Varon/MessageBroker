using MessageBroker.Outbound.Store;
using System;

namespace MessageBroker.Channel.Managment
{
    public class OutboundChannelsLifeCycle : IOutboundChannelsLifeCycle
    {
        private readonly IOutboundChannelStore _outboundChannelsStore;

        public OutboundChannelsLifeCycle(IOutboundChannelStore outboundChannelsStore)
        {
            _outboundChannelsStore = outboundChannelsStore ?? throw new ArgumentNullException(nameof(outboundChannelsStore));
        }

        public void InitOutboundChannel(string channelName)
        {
            _outboundChannelsStore.AddOutboundChannel(channelName);
        }

        public void TermOutboundChannels()
        {
            _outboundChannelsStore.RemoveAllChannelsAsync();
        }
    }
}