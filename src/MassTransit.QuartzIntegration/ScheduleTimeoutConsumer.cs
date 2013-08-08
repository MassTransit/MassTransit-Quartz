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
    using Logging;
    using Quartz;
    using Services.Timeout.Messages;


    public class ScheduleTimeoutConsumer :
        Consumes<ScheduleTimeout>.Context
    {
        static readonly ILog _log = Logger.Get<ScheduleMessageConsumer>();
        readonly IScheduler _scheduler;

        public ScheduleTimeoutConsumer(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Consume(IConsumeContext<ScheduleTimeout> context)
        {
            DateTime startTimeUtc = context.Message.TimeoutAt;
            if (startTimeUtc.Kind == DateTimeKind.Local)
                startTimeUtc = startTimeUtc.ToUniversalTime();

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("ScheduleTimeout: {0}[{1} at {2}", context.Message.CorrelationId,
                    context.Message.Tag,
                    startTimeUtc);
            }

            string triggerKey = context.Message.CorrelationId.ToString("N") + context.Message.Tag;

            var key = new TriggerKey(triggerKey);

            IJobDetail jobDetail = JobBuilder.Create<ScheduledMessageJob>()
                                             .RequestRecovery(true)
                                             .WithIdentity(context.Message.CorrelationId.ToString("N"))
                                             .UsingJobData("Tag", context.Message.Tag)
                                             .UsingJobData("CorrelationId", context.Message.CorrelationId.ToString())
                                             .Build();


            ITrigger trigger = TriggerBuilder.Create()
                                             .ForJob(jobDetail)
                                             .StartAt(startTimeUtc)
                                             .WithIdentity(key)
                                             .Build();

            if (_scheduler.CheckExists(key))
            {
                _scheduler.RescheduleJob(key, trigger);

                context.Bus.Publish(new TimeoutRescheduled
                    {
                        CorrelationId = context.Message.CorrelationId,
                        TimeoutAt = startTimeUtc,
                        Tag = context.Message.Tag,
                    });
            }
            else
            {
                _scheduler.ScheduleJob(jobDetail, trigger);

                context.Bus.Publish(new TimeoutScheduled
                    {
                        CorrelationId = context.Message.CorrelationId,
                        TimeoutAt = startTimeUtc,
                        Tag = context.Message.Tag,
                    });
            }
        }
    }
}