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
    using System.Globalization;
    using Logging;
    using Newtonsoft.Json;
    using Quartz;


    public class ScheduledMessageJob :
        IJob
    {
        static readonly ILog _log = Logger.Get<ScheduledMessageJob>();

        readonly IServiceBus _bus;

        public ScheduledMessageJob(IServiceBus bus)
        {
            _bus = bus;
        }

        public string Destination { get; set; }

        public string ExpirationTime { get; set; }
        public string ResponseAddress { get; set; }
        public string FaultAddress { get; set; }
        public string Body { get; set; }

        public string MessageId { get; set; }
        public string MessageType { get; set; }
        public string ContentType { get; set; }
        public string RequestId { get; set; }
        public string ConversationId { get; set; }
        public string CorrelationId { get; set; }
        public string Network { get; set; }
        public int RetryCount { get; set; }
        public string HeadersAsJson { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var destinationAddress = new Uri(Destination);
                Uri sourceAddress = _bus.Endpoint.Address.Uri;

                IEndpoint endpoint = _bus.GetEndpoint(destinationAddress);

                ISendContext messageContext = CreateMessageContext(sourceAddress, destinationAddress);

                endpoint.OutboundTransport.Send(messageContext);
            }
            catch (Exception ex)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "An exception occurred sending message {0} to {1}", MessageType, Destination);
                _log.Error(message, ex);

                throw new JobExecutionException(message, ex);
            }
        }


        ISendContext CreateMessageContext(Uri sourceAddress, Uri destinationAddress)
        {
            var context = new ScheduledMessageContext(Body);

            context.SetDestinationAddress(destinationAddress);
            context.SetSourceAddress(sourceAddress);
            context.SetResponseAddress(ToUri(ResponseAddress));
            context.SetFaultAddress(ToUri(FaultAddress));

            SetHeaders(context);
            context.SetMessageId(MessageId);
            context.SetRequestId(RequestId);
            context.SetConversationId(ConversationId);
            context.SetCorrelationId(CorrelationId);

            if (!string.IsNullOrEmpty(ExpirationTime))
                context.SetExpirationTime(DateTime.Parse(ExpirationTime));

            context.SetNetwork(Network);
            context.SetRetryCount(RetryCount);
            context.SetContentType(ContentType);

            return context;
        }

        void SetHeaders(ScheduledMessageContext context)
        {
            if (string.IsNullOrEmpty(HeadersAsJson))
                return;

            var headers = JsonConvert.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(HeadersAsJson);
            context.SetHeaders(headers);
        }

        static Uri ToUri(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            return new Uri(s);
        }
    }
}