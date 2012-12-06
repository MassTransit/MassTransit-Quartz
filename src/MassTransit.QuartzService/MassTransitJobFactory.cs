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
namespace MassTransit.QuartzService
{
    using System;
    using System.Globalization;
    using Magnum.Extensions;
    using Quartz;
    using Quartz.Spi;


    public class MassTransitJobFactory :
        IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            IJobDetail jobDetail = bundle.JobDetail;
            if (jobDetail == null)
                throw new SchedulerException("JobDetail was null");

            Type type = jobDetail.JobType;
            object obj;
            try
            {
                obj = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                var sex = new SchedulerException(string.Format(CultureInfo.InvariantCulture,
                    "Problem instantiating class '{0}'", jobDetail.JobType.FullName), ex);
                throw sex;
            }

            var job = obj as IJob;
            if (job == null)
            {
                throw new SchedulerException(string.Format(CultureInfo.InvariantCulture,
                    "The job class does not implement IJob '{0}'", type.ToShortTypeName()));
            }

            return job;
        }
    }
}