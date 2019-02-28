namespace MessageBroker.Channel.Managment
{
    public interface IOutboundChannelsLifeCycle
    {
        void InitOutboundChannel(string channelName);
        void TermOutboundChannels();
    }
}