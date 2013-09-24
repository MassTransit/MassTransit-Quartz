// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.QuartzIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Context;
    using Serialization.Custom;


    public class ScheduledMessageContext :
        MessageContext,
        ISendContext
    {
        readonly string _body;

        public ScheduledMessageContext(string body)
        {
            _body = body;
            Id = NewId.NextGuid();
            DeclaringMessageType = typeof(ScheduledMessageContext);
        }

        public void SerializeTo(Stream stream)
        {
            using (var nonClosingStream = new NonClosingStream(stream))
            using (var writer = new StreamWriter(nonClosingStream))
            {
                writer.Write(_body);
            }
        }

        public void SetHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var entry in headers)
                SetHeader(entry.Key, entry.Value);
        }

        public bool TryGetContext<T>(out IBusPublishContext<T> context)
            where T : class
        {
            context = null;
            return false;
        }

        public void NotifySend(IEndpointAddress address)
        {
        }

        public Guid Id { get; private set; }
        public Type DeclaringMessageType { get; private set; }
    }
}