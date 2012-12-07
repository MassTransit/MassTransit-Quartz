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
    using Quartz;
    using Scheduling;


    public class ScheduleMessageJobBuilderImpl<T> :
        ScheduleMessageJobBuilder
        where T : class
    {
        public void Consume(IScheduler scheduler, IConsumeContext<ScheduleMessage> context)
        {
            IConsumeContext<ScheduleMessage<T>> consumeContext;
            if (context.TryGetContext(out consumeContext))
            {
            }

            throw new InvalidOperationException("The message context could not be mapped: {0}" + typeof(T));
        }
    }
}