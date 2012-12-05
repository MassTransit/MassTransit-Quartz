﻿// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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


    public interface ScheduleMessage :
        CorrelatedBy<Guid>
    {
        /// <summary>
        /// The time at which the message should be published
        /// </summary>
        DateTime ScheduledTime { get; }
    }


    public interface ScheduleMessage<out T> :
        ScheduleMessage
        where T : class
    {
        /// <summary>
        /// The message to be published 
        /// </summary>
        T Payload { get; }
    }
}