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
    using System.Globalization;
    using Logging;
    using Quartz;
    using Services.Timeout.Messages;


    public class ScheduledTimeoutJob :
        IJob
    {
        static readonly ILog _log = Logger.Get<ScheduledTimeoutJob>();

        readonly IServiceBus _bus;

        public ScheduledTimeoutJob(IServiceBus bus)
        {
            _bus = bus;
        }


        public string CorrelationId { get; set; }
        public int Tag { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                _bus.Publish(new TimeoutExpired
                    {
                        CorrelationId = Guid.Parse(CorrelationId),
                        Tag = Tag,
                    });
            }
            catch (Exception ex)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "An exception occurred publishing scheduled timeout {0}[{1}]", CorrelationId, Tag);
                _log.Error(message, ex);

                throw new JobExecutionException(message, ex);
            }
        }
    }
}