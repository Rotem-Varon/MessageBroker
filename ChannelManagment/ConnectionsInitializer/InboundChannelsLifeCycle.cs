using MessageBroker.Inbound.Store;
using System;
using System.Collections.Generic;

namespace MessageBroker.Channel.Managment
{
    public class InboundChannelsLifeCycle : IInboundChannelsLifeCycle
    {
        private readonly IInboundChannelStore _inboundChannelsStore;

        public InboundChannelsLifeCycle(IInboundChannelStore inboundChannelsStore)
        {
            _inboundChannelsStore = inboundChannelsStore ?? throw new ArgumentNullException(nameof(inboundChannelsStore));
        }

        public void InitInboundChannels(string channelName, List<string> subscriptionNames, ushort maxConncurentHandlers)
        {
            foreach (var subscriptionName in subscriptionNames)
            {
                _inboundChannelsStore.AddInboundChannel(channelName, subscriptionName, maxConncurentHandlers);
            }
        }

        public void TermInboundChannels()
        {
            _inboundChannelsStore.RemoveAllChannelsAsync();
        }
    }
}