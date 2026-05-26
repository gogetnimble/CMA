using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cma.Services.EventManagement
{
    public interface IAventriService
    {
        public Task<EventResponse?> GetEvent(string eventId);

        public Task<AttendeeResponse?> GetAttendee(string eventId, string attendeeId);
    }
}
