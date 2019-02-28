using MessageBroker.Messaging.Extensions;
using System;
using System.Collections.Generic;

namespace MessageBroker.Messaging
{
    public class TypedMessage
    {
        public Type BodyType => Body.GetType();

        private object _body;

        public object Body
        {
            get { return _body; }
            set
            {
                _body = value;
                MessageType = _body.GetMessageType();
            }
        }

        public string MessageType { get; set; }
        public ushort MessageVersion { get; set; }

        public TBody BodyAs<TBody>()
        {
            return (TBody)Body;
        }

        public IReadOnlyDictionary<string, object> CustomProperties { get; set; }
    }
}
