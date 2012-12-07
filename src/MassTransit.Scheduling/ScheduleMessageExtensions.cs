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
namespace MassTransit.Scheduling
{
    using System;


    /// <summary>
    /// Extensions for scheduling publish/send message 
    /// </summary>
    public static class ScheduleMessageExtensions
    {
        public static ScheduledMessage<T> SchedulePublish<T>(this IServiceBus bus, DateTime scheduledTime, T message)
            where T : class
        {
            var scheduleMessage = new ScheduleMessageCommand<T>(scheduledTime, message);

            bus.Publish(scheduleMessage);

            return new ScheduledMessageHandle<T>(scheduleMessage.CorrelationId, scheduleMessage.ScheduledTime,
                scheduleMessage.Payload);
        }

        public static ScheduledMessage<T> ScheduleSend<T>(this IEndpoint endpoint, DateTime scheduledTime, T message)
            where T : class
        {
            var command = new ScheduleMessageCommand<T>(scheduledTime, message);

            endpoint.Send(command);

            return new ScheduledMessageHandle<T>(command.CorrelationId, command.ScheduledTime,
                command.Payload);
        }

        /// <summary>
        /// Cancel a scheduled message using the scheduled message instance
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="message"> </param>
        public static void CancelScheduledMessage<T>(this IServiceBus bus, ScheduledMessage<T> message)
            where T : class
        {
            if (message == null)
                throw new ArgumentNullException("message");

            CancelScheduledMessage(bus, message.TokenId);
        }

        /// <summary>
        /// Cancel a scheduled message using the tokenId that was returned when the message was scheduled.
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="tokenId">The tokenId of the scheduled message</param>
        public static void CancelScheduledMessage(this IServiceBus bus, Guid tokenId)
        {
            var command = new CancelScheduledMessageCommand(tokenId);

            bus.Publish(command);
        }


        class CancelScheduledMessageCommand :
            CancelScheduledMessage
        {
            public CancelScheduledMessageCommand(Guid tokenId)
            {
                CorrelationId = NewId.NextGuid();
                Timestamp = DateTime.UtcNow;

                TokenId = tokenId;
            }

            public Guid TokenId { get; private set; }
            public DateTime Timestamp { get; private set; }
            public Guid CorrelationId { get; private set; }
        }


        class ScheduleMessageCommand<T> :
            ScheduleMessage<T>
            where T : class
        {
            public ScheduleMessageCommand(DateTime scheduledTime, T payload)
            {
                CorrelationId = NewId.NextGuid();

                ScheduledTime = scheduledTime.Kind == DateTimeKind.Local
                                    ? scheduledTime.ToUniversalTime()
                                    : scheduledTime;

                Payload = payload;
            }

            public Guid CorrelationId { get; private set; }
            public DateTime ScheduledTime { get; private set; }
            public T Payload { get; private set; }
        }


        class ScheduledMessageHandle<T> :
            ScheduledMessage<T>
            where T : class
        {
            public ScheduledMessageHandle(Guid tokenId, DateTime scheduledTime, T payload)
            {
                TokenId = tokenId;
                ScheduledTime = scheduledTime;
                Payload = payload;
            }

            public Guid TokenId { get; private set; }
            public DateTime ScheduledTime { get; private set; }
            public T Payload { get; private set; }
        }
    }
}