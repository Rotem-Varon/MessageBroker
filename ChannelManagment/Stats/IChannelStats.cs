using System.Collections.Generic;
using System.Threading.Tasks;

namespace MessageBroker.Channel.Managment
{
    public interface IChannelStats
    {
        Task<string> GetMessageCountForChannelPerSubscriptionAsync(string channel);
        Task<long> GetMessageCountForSubscriptionAsync(string channel, string sub);
        Task<string> GetMessageCountForAllChannelsPerSubscription(IList<string> channels);
    }
}