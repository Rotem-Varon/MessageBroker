using LoggingProvider.Generic;
using MessageBroker.Common;
using MessageBroker.Messaging.Outbound;
using System;
using System.Collections.Concurrent;

namespace MessageBroker.Outbound.Store
{
    public sealed class OutboundChannelStore : IOutboundChannelStore
    {
        readonly string _connectionString;
        readonly ILogger _logger;


        readonly ConcurrentDictionary<string, IMessageSender> _outboundChannels =
            new ConcurrentDictionary<string, IMessageSender>();

        public OutboundChannelStore(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(OutboundChannelStore) + nameof(logger));
            _connectionString = connectionString;
            _outboundChannels = new ConcurrentDictionary<string, IMessageSender>();
        }

        public void AddOutboundChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name is null or empty", nameof(channelName));

            //todo: consider injecting a factory to decouple
            var connection = new ChannelPublisher(_connectionString, channelName, _logger);
            if (connection == null) throw new ArgumentNullException($"Failed to add connection. Channel: [{channelName}]");
            if (!_outboundChannels.TryAdd(channelName, connection))
                throw new Exception($"Failed to add connection. Connection Id: [{channelName}], Subscription: [{connection}]");
        }

        public Maybe<IMessageSender> GetOutboundChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name is null or empty", nameof(channelName));

            IMessageSender outboundChannel;
            return _outboundChannels.TryGetValue(channelName, out outboundChannel) ?
                new Maybe<IMessageSender>(outboundChannel) :
                new Maybe<IMessageSender>();
        }

        public void RemoveAllChannelsAsync()
        {
            foreach (var outboundChannel in _outboundChannels)
            {
                outboundChannel.Value.TerminateAsync();
            }
            _outboundChannels.Clear();
        }
    }
}