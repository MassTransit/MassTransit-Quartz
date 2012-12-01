MassTransit-Quartz
==================

Quartz support for MassTransit

Possible API
==============

Create a service that will allow messages to be scheduled.

For example,

var handle = bus.SchedulePublish(30.Seconds().FromNow, new MyMessage(someId, someValue));

The default implementation can be in memory, but we can also serialize the message to Json and store it for later resubmission via the bus.

Should probably have a SchedulePublish() and a ScheduleSend(endpoint, ...) so that direct sends as well as publishes can be supported, including forging the sender address to be that of the original requestor (instead of the scheduled message service).

Guid tokenId = handle.TokenId;

bus.CancelScheduledMessage(tokenId);
or handle.Cancel(); if the handle is still around (just a closure around the bus and method).