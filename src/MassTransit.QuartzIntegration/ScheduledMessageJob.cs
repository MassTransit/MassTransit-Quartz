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
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Quartz;
    using Serialization;
    using Serialization.Custom;


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

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var destinationAddress = new Uri(Destination);
                Uri sourceAddress = _bus.Endpoint.Address.Uri;

                IEndpoint endpoint = _bus.GetEndpoint(destinationAddress);


                if (string.Compare(ContentType, "application/vnd.masstransit+json", StringComparison.OrdinalIgnoreCase)
                    == 0)
                {
                    Console.WriteLine(Body);
                    TranslateJsonBody();
                    Console.WriteLine(Body);
                }
                else if (string.Compare(ContentType, "application/vnd.masstransit+xml",
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Console.WriteLine(Body);
                    TranslateXmlBody();
                    Console.WriteLine(Body);
                }
                else
                    throw new InvalidOperationException("Only JSON and XML messages can be scheduled");

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


        public ISendContext CreateMessageContext(Uri sourceAddress, Uri destinationAddress)
        {
            var context = new ScheduledMessageContext(Body);

            context.SetDestinationAddress(destinationAddress);
            context.SetSourceAddress(sourceAddress);
            context.SetResponseAddress(ToUri(ResponseAddress));
            context.SetFaultAddress(ToUri(FaultAddress));

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

        Uri ToUri(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            return new Uri(s);
        }

        void TranslateJsonBody()
        {
            var envelope = JObject.Parse(Body);

            envelope["destinationAddress"] = Destination;

            JToken message = envelope["message"];

            JToken payload = message["payload"];
            var payloadType = message["payloadType"];
            
            envelope["message"] = payload;
            envelope["messageType"] = payloadType;

            Body = JsonConvert.SerializeObject(envelope, Formatting.Indented);
        }

        void TranslateXmlBody()
        {
            using (var reader = new StringReader(Body))
            {
                XDocument document = XDocument.Load(reader);

                XElement envelope = (from e in document.Descendants("envelope") select e).Single();

                XElement destinationAddress = (from a in envelope.Descendants("destinationAddress") select a).Single();

                XElement message = (from m in envelope.Descendants("message") select m).Single();
                IEnumerable<XElement> messageType = (from mt in envelope.Descendants("messageType") select mt);

                XElement payload = (from p in message.Descendants("payload") select p).Single();
                IEnumerable<XElement> payloadType = (from pt in message.Descendants("payloadType") select pt);

                message.Remove();
                messageType.Remove();

                destinationAddress.Value = Destination;

                message = new XElement("message");
                message.Add(payload.Descendants());
                envelope.Add(message);

                envelope.Add(payloadType.Select(x => new XElement("messageType", x.Value)));

                Body = document.ToString();
            }
        }
    }
}