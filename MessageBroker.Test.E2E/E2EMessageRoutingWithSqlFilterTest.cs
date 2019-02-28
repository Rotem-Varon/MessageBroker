/*Consolidate test files (E2ETest)*/
using LoggingProvider.AppInsights;
using LoggingProvider.Generic;
using MessageBroker.Channel.Managment;
using MessageBroker.Inbound.Store;
using MessageBroker.Messaging;
using MessageBroker.Outbound.Store;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Test.E2E
{
    public sealed class E2EMessageRoutingWithSqlFilterTest
    {
        ILogger _logger;
        IChannelStats _channelStats;
        IInboundChannelsLifeCycle _inboundChannelsLifeCycle;
        IOutboundChannelsLifeCycle _outboundChannelsLifeCycle;
        IInboundChannelStore _inboundChannelStore;
        IOutboundChannelStore _outboundChannelStore;

        string _connectionString = "";
        ushort _maxConcurrentReaders = 10;

        string _messageBrokerFilterTestTopic = "MessageBrokerE2EWithSqlFilterTestTopic" + DateTime.Now.Ticks;
        string _messageBrokerAllMsgsTestSub = "MessageBrokerAllMsgsTestSub";
        string _messageBrokerFilteredMsgsTestSub = "MessageBrokerFilteredMsgsTestSub";
        string _messageBrokerSubSqlFilterQuery = "Filtered=True";

        const int ProcessingTime = 3000;

        [SetUp]
        public void Init()
        {
            _logger = new LoggingProviderAppInsights(nameof(E2EMessageRoutingWithSqlFilterTest) + nameof(Init));

            _inboundChannelStore = new InboundChannelStore(_connectionString, _logger);
            _outboundChannelStore = new OutboundChannelStore(_connectionString, _logger);

            //todo: use async await pervasively. see comment in ChannelSubscriber
            CreateMessagingEntities();
            InitializeInboundChannels();
            InitializeOutboundChannels();
            InitializeChannelStatistics();
        }

        [TearDown]
        public void Cleanup()
        {
            TermInboundChannels();
            TermOutboundChannels();
            //Thread.Sleep(70000); //time for logs to flush appInsights
        }

        private void CreateMessagingEntities()
        {
            var messagingEntitiesFactory = new MessagingEntitiesFactory(_connectionString, _logger);
            var builder = new MessagingEntitiesBuilder(messagingEntitiesFactory);
            builder.BuildMessagingEntitiesStructure(_messageBrokerFilterTestTopic, _messageBrokerAllMsgsTestSub);
            builder.BuildMessagingEntitiesStructure(_messageBrokerFilterTestTopic, _messageBrokerFilteredMsgsTestSub, _messageBrokerSubSqlFilterQuery);
        }

        private void InitializeChannelStatistics()
        {
            _channelStats = new ChannelStats(_connectionString, _logger);
        }

        private void InitializeInboundChannels()
        {
            _inboundChannelsLifeCycle = new InboundChannelsLifeCycle(_inboundChannelStore);
            _inboundChannelsLifeCycle.InitInboundChannels(_messageBrokerFilterTestTopic, new List<string>() { _messageBrokerAllMsgsTestSub, _messageBrokerFilteredMsgsTestSub }, _maxConcurrentReaders);
        }

        private void InitializeOutboundChannels()
        {
            _outboundChannelsLifeCycle = new OutboundChannelsLifeCycle(_outboundChannelStore);
            _outboundChannelsLifeCycle.InitOutboundChannel(_messageBrokerFilterTestTopic);
        }

        private void TermInboundChannels()
        {
            _inboundChannelsLifeCycle.TermInboundChannels();
        }

        private void TermOutboundChannels()
        {
            _outboundChannelsLifeCycle.TermOutboundChannels();
        }

        [TestCase(3, 3)]
        public async Task FilteredSubCosumesOnlyRightMessages(int numOfUnfilteredTestMsgs, int numOfFilteredTestMsgs)
        {
            var numOfAllTestMsgs = numOfUnfilteredTestMsgs + numOfFilteredTestMsgs;
            //we expect no msgs on newly created subscriptions
            var subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerAllMsgsTestSub);
            Assert.AreEqual(0, subMsgCount);
            subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerFilteredMsgsTestSub);
            Assert.AreEqual(0, subMsgCount);

            //send msgs
            var testAllMsgIds = new List<Guid>();
            var testFilteredMsgIds = new List<Guid>();
            var testMsgSender = _outboundChannelStore.GetOutboundChannel(_messageBrokerFilterTestTopic).Single();
            var random = new Random();

            //send unfiltered msgs that won't be routed to the filtered subscription
            for (int i = 0; i < numOfUnfilteredTestMsgs; i++)
            {
                var testMsgId = Guid.NewGuid();

                var testMsg = new TestMsgDto()
                {
                    CorrelationId = testMsgId,
                    Temperature = random.Next()
                };
                var properties = new Dictionary<string, object>();
                properties.Add("Filtered", false);

                await testMsgSender.SendAsync(new TypedMessage
                {
                    Body = testMsg,
                    CustomProperties = properties
                });
                testAllMsgIds.Add(testMsgId);
            }

            for (int i = 0; i < numOfUnfilteredTestMsgs; i++)
            {
                subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerAllMsgsTestSub);
                if (subMsgCount > 0)
                    Thread.Sleep(ProcessingTime);
                else
                    break;
            }

            //send filtered msgs that will be routed to the filtered subscription
            for (int i = 0; i < numOfFilteredTestMsgs; i++)
            {
                var testMsgId = Guid.NewGuid();

                var testMsg = new TestMsgDto()
                {
                    CorrelationId = testMsgId,
                    Temperature = random.Next()
                };
                var properties = new Dictionary<string, object>();
                properties.Add("Filtered", true);

                await testMsgSender.SendAsync(new TypedMessage
                {
                    Body = testMsg,
                    CustomProperties = properties
                });
                testAllMsgIds.Add(testMsgId);
                testFilteredMsgIds.Add(testMsgId);
            }

            for (int i = 0; i < numOfFilteredTestMsgs; i++)
            {
                subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerFilteredMsgsTestSub);
                if (subMsgCount > 0)
                    Thread.Sleep(ProcessingTime);
                else
                    break;
            }

            //check the amount of messages in the filtered subscription
            subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerFilteredMsgsTestSub);
            Assert.AreEqual(numOfFilteredTestMsgs, subMsgCount);

            //check if all messages have been sent successfully 
            subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerAllMsgsTestSub);
            Assert.AreEqual(numOfAllTestMsgs, subMsgCount);

            var channelSubscriber =
                _inboundChannelStore.GetChannelSubscriber(_messageBrokerFilterTestTopic, _messageBrokerFilteredMsgsTestSub).Single();
            channelSubscriber.ListenAsync<TestMsgDto>((msg) =>
            {
                if (testFilteredMsgIds.Contains(msg.CorrelationId))
                    testFilteredMsgIds.Remove(msg.CorrelationId);
                return Task.CompletedTask;
            });

            for (int i = 0; i < numOfFilteredTestMsgs; i++)
            {
                if (testFilteredMsgIds.Count > 0)
                    Thread.Sleep(ProcessingTime);
                else
                    break;
            }

            channelSubscriber =
                _inboundChannelStore.GetChannelSubscriber(_messageBrokerFilterTestTopic, _messageBrokerAllMsgsTestSub).Single();
            channelSubscriber.ListenAsync<TestMsgDto>((msg) =>
            {
                if (testAllMsgIds.Contains(msg.CorrelationId))
                    testAllMsgIds.Remove(msg.CorrelationId);
                return Task.CompletedTask;
            });

            for (int i = 0; i < numOfAllTestMsgs; i++)
            {
                if (testAllMsgIds.Count > 0)
                    Thread.Sleep(ProcessingTime);
                else
                    break;
            }

            //we expect all msgs read at this point for both subscriptions
            subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerFilteredMsgsTestSub);
            Assert.AreEqual(0, subMsgCount);
            subMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerFilterTestTopic, _messageBrokerAllMsgsTestSub);
            Assert.AreEqual(0, subMsgCount);
        }
    }
}
