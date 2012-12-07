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
    using System.Linq.Expressions;
    using System.Reflection;
    using Magnum.Caching;
    using Magnum.Extensions;
    using Quartz;
    using Quartz.Spi;


    public class MassTransitJobFactory :
        IJobFactory
    {
        readonly IServiceBus _bus;
        readonly Cache<Type, IJobFactory> _typeFactories;

        public MassTransitJobFactory(IServiceBus bus)
        {
            _bus = bus;
            _typeFactories = new GenericTypeCache<IJobFactory>(typeof(MassTransitJobFactory<>), type =>
                {
                    Type genericType = typeof(MassTransitJobFactory<>).MakeGenericType(type);

                    return (IJobFactory)Activator.CreateInstance(genericType, _bus);
                });
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            IJobDetail jobDetail = bundle.JobDetail;
            if (jobDetail == null)
                throw new SchedulerException("JobDetail was null");

            Type type = jobDetail.JobType;

            return _typeFactories[type].NewJob(bundle, scheduler);
        }
    }


    public class MassTransitJobFactory<T> :
        IJobFactory
        where T : IJob
    {
        readonly IServiceBus _bus;
        Func<IServiceBus, IJob> _factory;

        public MassTransitJobFactory(IServiceBus bus)
        {
            _bus = bus;
            _factory = CreateConstructor();
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            try
            {
                return _factory(_bus);
            }
            catch (Exception ex)
            {
                var sex = new SchedulerException(string.Format(CultureInfo.InvariantCulture,
                    "Problem instantiating class '{0}'", bundle.JobDetail.JobType.FullName), ex);
                throw sex;
            }
        }

        Func<IServiceBus, IJob> CreateConstructor()
        {
            ConstructorInfo ctor = typeof(T).GetConstructor(new[] {typeof(IServiceBus)});
            if (ctor != null)
                return CreateServiceBusConstructor(ctor);

            ctor = typeof(T).GetConstructor(Type.EmptyTypes);
            if (ctor != null)
                return CreateDefaultConstructor(ctor);

            throw new SchedulerException(string.Format(CultureInfo.InvariantCulture,
                "The job class does not have a supported constructor: {0}", typeof(T).ToShortTypeName()));
        }

        Func<IServiceBus, IJob> CreateDefaultConstructor(ConstructorInfo constructorInfo)
        {
            ParameterExpression bus = Expression.Parameter(typeof(IServiceBus), "bus");
            NewExpression @new = Expression.New(constructorInfo);

            return Expression.Lambda<Func<IServiceBus, IJob>>(@new, bus).Compile();
        }

        Func<IServiceBus, IJob> CreateServiceBusConstructor(ConstructorInfo constructorInfo)
        {
            ParameterExpression bus = Expression.Parameter(typeof(IServiceBus), "bus");
            NewExpression @new = Expression.New(constructorInfo, bus);

            return Expression.Lambda<Func<IServiceBus, IJob>>(@new, bus).Compile();
        }
    }
}