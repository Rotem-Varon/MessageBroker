using System.Collections.Generic;

namespace MessageBroker.Channel.Managment
{
    public interface IInboundChannelsLifeCycle
    {
        void InitInboundChannels(string channelName, List<string> subscriptionNames, ushort maxNumOfHandlers);
        void TermInboundChannels();
    }
}