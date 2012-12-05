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
    using System.IO;
    using System.Text;
    using Logging;
    using Quartz;
    using Scheduling;


    public class ScheduleMessageConsumer :
        Consumes<ScheduleMessage>.Context
    {
        static readonly ILog _log = Logger.Get<ScheduleMessageConsumer>();

        readonly IScheduler _scheduler;

        public ScheduleMessageConsumer(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Consume(IConsumeContext<ScheduleMessage> context)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("ScheduleMessage: {0} at {1}", context.Message.CorrelationId,
                    context.Message.ScheduledTime);

            string body;
            using (var ms = new MemoryStream())
            {
                context.BaseContext.CopyBodyTo(ms);

                body = Encoding.UTF8.GetString(ms.ToArray());
            }

            IJobDetail jobDetail = JobBuilder.Create<PublishJob>()
                                             .RequestRecovery(true)
                                             .WithIdentity(context.Message.CorrelationId.ToString("N"))
                                             .UsingJobData("body", body)
                                             .UsingJobData("sourceAddress", context.SourceAddress.ToString())
                                             .UsingJobData("faultAddress", (context.FaultAddress ?? context.SourceAddress).ToString())
                                             .StoreDurably(true)
                                             .Build();

            ITrigger trigger = TriggerBuilder.Create()
                                             .ForJob(jobDetail)
                                             .StartAt(context.Message.ScheduledTime)
                                             .Build();

            _scheduler.ScheduleJob(jobDetail, trigger);
        }
    }
}