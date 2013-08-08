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
    using Logging;
    using Quartz;
    using Services.Timeout.Messages;


    public class CancelScheduledTimeoutConsumer :
        Consumes<CancelTimeout>.Context
    {
        static readonly ILog _log = Logger.Get<CancelScheduledTimeoutConsumer>();
        readonly IScheduler _scheduler;

        public CancelScheduledTimeoutConsumer(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Consume(IConsumeContext<CancelTimeout> context)
        {
            string triggerKey = context.Message.CorrelationId.ToString("N") + context.Message.Tag;

            bool unscheduledJob = _scheduler.UnscheduleJob(new TriggerKey(triggerKey));

            if (_log.IsDebugEnabled)
            {
                if (unscheduledJob)
                {
                    _log.DebugFormat("CancelScheduledMessage: {0}", triggerKey);

                    context.Bus.Publish(new TimeoutCancelled
                    {
                        CorrelationId = context.Message.CorrelationId,
                        Tag = context.Message.Tag,
                    });
                }
                else
                    _log.DebugFormat("CancelScheduledMessage: no message found {0}", triggerKey);
            }
        }
    }
}