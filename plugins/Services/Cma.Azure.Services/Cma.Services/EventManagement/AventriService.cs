using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Payment.Bambora;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cma.Services.EventManagement
{
    public class AventriService : IAventriService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AventriService> _logger;


        public AventriService(IHttpClientFactory httpClientFactory, ILogger<AventriService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(nameof(AventriService));
            _configuration = configuration;
        }

        public async Task<EventResponse?> GetEvent(string eventId)
        {            
            _logger.LogInformation($"Fetching event details from Aventri for eventId: {eventId}");

            var accessToken = _configuration.GetSection("Aventri")["AccessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError($"Aventri Access Token not Configured");
                return null;
            }

            string eventEndpoint = $"api/v2/ereg/getEvent.json?accesstoken={accessToken}&eventid={eventId}";

            var response = await _httpClient.GetAsync(eventEndpoint);
            var eventJsonResponse = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(eventJsonResponse);


            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Event details fetching failed for eventId: {eventId}");

                return default;
            }

            var eventResponse = JsonConvert.DeserializeObject<EventResponse>(eventJsonResponse);

            return eventResponse;
        }

        public async Task<AttendeeResponse?> GetAttendee(string eventId,string attendeeId)
        {
            _logger.LogInformation($"Fetching attendee details from Aventri for attendeeId: {attendeeId}");

            var accessToken = _configuration.GetSection("Aventri")["AccessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError($"Aventri Access Token not Configured");
                return null;
            }

            string attendeeEndpoint = $"api/v2/ereg/getAttendee.json?accesstoken={accessToken}&eventid={eventId}&attendeeid={attendeeId}";

            var response = await _httpClient.GetAsync(attendeeEndpoint);
            var attendeeJsonResponse = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(attendeeJsonResponse);


            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Event details fetching failed for eventId: {eventId}");

                return default;
            }

            var attendeeResponse = JsonConvert.DeserializeObject<AttendeeResponse>(attendeeJsonResponse);

            return attendeeResponse;
        }
    }
}
