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
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Magnum.Caching;
    using Magnum.Extensions;
    using Magnum.Reflection;
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

        public void ReturnJob(IJob job)
        {
        }
    }


    public class MassTransitJobFactory<T> :
        IJobFactory
        where T : IJob
    {
        readonly IServiceBus _bus;
        readonly Func<IServiceBus, T> _factory;
        readonly Cache<string, FastProperty<T>> _propertyCache;

        public MassTransitJobFactory(IServiceBus bus)
        {
            _bus = bus;
            _factory = CreateConstructor();

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            _propertyCache = new DictionaryCache<string, FastProperty<T>>(typeof(T).GetProperties(Flags)
                .Where(x => x.GetGetMethod(true) != null)
                .Where(x => x.GetSetMethod(true) != null)
                .Select(x => new FastProperty<T>(x, Flags))
                .ToDictionary(x => x.Property.Name));
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            try
            {
                T job = _factory(_bus);

                var jobData = new JobDataMap();
                jobData.PutAll(scheduler.Context);
                jobData.PutAll(bundle.JobDetail.JobDataMap);
                jobData.PutAll(bundle.Trigger.JobDataMap);

                SetObjectProperties(job, jobData);

                return job;
            }
            catch (Exception ex)
            {
                var sex = new SchedulerException(string.Format(CultureInfo.InvariantCulture,
                    "Problem instantiating class '{0}'", bundle.JobDetail.JobType.FullName), ex);
                throw sex;
            }
        }

        public void ReturnJob(IJob job)
        {
        }

        void SetObjectProperties(T job, JobDataMap jobData)
        {
            foreach (string key in jobData.Keys)
            {
                if (_propertyCache.Has(key))
                {
                    FastProperty<T> property = _propertyCache[key];

                    object value = jobData[key];

                    if (property.Property.PropertyType == typeof(Uri))
                        value = new Uri(value.ToString());

                    property.Set(job, value);
                }
            }
        }

        Func<IServiceBus, T> CreateConstructor()
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

        Func<IServiceBus, T> CreateDefaultConstructor(ConstructorInfo constructorInfo)
        {
            ParameterExpression bus = Expression.Parameter(typeof(IServiceBus), "bus");
            NewExpression @new = Expression.New(constructorInfo);

            return Expression.Lambda<Func<IServiceBus, T>>(@new, bus).Compile();
        }

        Func<IServiceBus, T> CreateServiceBusConstructor(ConstructorInfo constructorInfo)
        {
            ParameterExpression bus = Expression.Parameter(typeof(IServiceBus), "bus");
            NewExpression @new = Expression.New(constructorInfo, bus);

            return Expression.Lambda<Func<IServiceBus, T>>(@new, bus).Compile();
        }
    }
}