using MediatR;

namespace Cma.Application.Feature.Aventri
{
    public record AttendeeEventRequest(List<AttendeeEvent> AttendeeEvents) :IRequest<AttendeeEventResponse>
    {
        public List<AttendeeEvent> AttendeeEvents { get; set; } = AttendeeEvents;

    }

    public class AttendeeEvent
    {
        public string? AttendeeId { get; set; }
        public string? EventId { get; set; }
        public string? ParentId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Status { get; set; }
    }
}
