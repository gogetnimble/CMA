using Cma.Services.Crm;
using Cma.Services.Crm.Contracts;
using Cma.Services.EventManagement;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cma.Application.Feature.Aventri
{
    public class AttendeeEventHandler : IRequestHandler<AttendeeEventRequest, AttendeeEventResponse>
    {
        private readonly ILogger<AttendeeEventHandler> _logger;
        private readonly ICrmService _crmService;
        private readonly IAventriService _aventriService;
        public AttendeeEventHandler(ICrmService crmService, IAventriService aventriService, ILogger<AttendeeEventHandler> logger)
        {
            _crmService = crmService;
            _aventriService = aventriService;
            _logger = logger;
        }
        public async Task<AttendeeEventResponse> Handle(AttendeeEventRequest request, CancellationToken cancellationToken = default)
        {
            EventResponse? eventResponse = null;

            //Validate if all events in the request are the same, if not log a warning and return
            // bool allSameEvent = request.AttendeeEvents.Select(e => e.EventId).Distinct().Count() == 1;


            foreach (var attendeeEvent in request.AttendeeEvents)
            {
                _logger.LogInformation("Processing attendee event for email: {Email} and eventId: {EventName}", attendeeEvent.Email, attendeeEvent.EventId);

                //Get Event Details from Aventri

                eventResponse = await _aventriService.GetEvent(attendeeEvent.EventId!);

                if (eventResponse == null) { continue; }


                //Get Attendee Details from Aventri
                var attendeeResponse = await _aventriService.GetAttendee(attendeeEvent.EventId!, attendeeEvent.AttendeeId!);

                if (attendeeResponse == null) { continue; }

                //upsert event in CRM
                var eventUpserted = await UpsertEventInCrm(attendeeEvent, eventResponse!);
            }


            return new AttendeeEventResponse();
        }

        public async Task<Guid?> UpsertEventInCrm(AttendeeEvent request, EventResponse eventResponse)
        {
            //check if event exists in CRM
            var eventInCrm = await _crmService.FindEntityBy(new(CrmConstants.Event.EntityName, new()
                        {
                            { CrmConstants.Event.EventId, request.EventId! }

                        }, true));

            if (eventInCrm is not null)
            {
                //update event in CRM
                var eventProperties = new List<EntityProperty>
                {
                new(CrmConstants.Event.Name, eventResponse.name),
                new(CrmConstants.Event.StartDate, eventResponse.startdate),
                new(CrmConstants.Event.EndDate, eventResponse.enddate),                

                };
                await _crmService.SetEventProperties(new SetEntityPropertiesRequest(eventInCrm.Id, eventProperties));
                return eventInCrm.Id;
            }
            else
            {
                //Create event in CRM
                //var eventProperties = new List<EntityProperty>
                //{
                //    new(CrmConstants.Event.EventId, request.EventId!),
                //    new(CrmConstants.Event.EventName, eventResponse.Name!),
                //    new(CrmConstants.Event.EventDate, eventResponse.Date)
                //};
            }


            return default;
        }
    }
}
