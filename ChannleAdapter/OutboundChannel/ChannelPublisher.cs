using LoggingProvider.Generic;
using MessageBroker.Messaging.Extensions;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Messaging.Outbound
{
    public sealed class ChannelPublisher : ChannelFactory, IMessageSender
    {
        readonly TopicClient _channelClient;
        readonly string _channelName;

        public ChannelPublisher(string connectionString, string channelName, ILogger logger)
            : base(connectionString, logger)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name was not provided");
            _channelName = channelName;
            _channelClient = _factory.CreateTopicClient(channelName);
        }

        public async Task SendAsync(TypedMessage typedMessage)
        {
            _logger.LogInfo($"Sending Message: {typedMessage.ToJsonString()}. Channel info: {_channelName}");

            try
            {
                byte[] msgBodyInBytes = Encoding.UTF8.GetBytes(typedMessage.Body.ToJsonString());
                MemoryStream stream = new MemoryStream(msgBodyInBytes, writable: false);
                var brokeredMessage = new BrokeredMessage(stream) { ContentType = "application/json" };
                if (typedMessage.CustomProperties != null)
                {
                    foreach (var property in typedMessage.CustomProperties)
                    {
                        brokeredMessage.Properties.Add(property);
                    }
                }
                await _channelClient.SendAsync(brokeredMessage);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception: {e.GetBaseException().Message}. Channel info: {_channelName}");
                throw;
            }
        }

        [ExcludeFromCodeCoverage]
        public async Task TerminateAsync()
        {
            if (_channelClient != null && !_channelClient.IsClosed)
                await _channelClient.CloseAsync();
        }
    }
}
