﻿using Newtonsoft.Json;

namespace MessageBroker.Messaging.Extensions
{
    public static class ObjectExtensions
    {
        public static string GetMessageType(this object obj)
        {
            return obj.GetType().AssemblyQualifiedName;
        }

        public static string ToJsonString(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}