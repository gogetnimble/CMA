using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cma.Services.EventManagement
{
    public class AttendeeResponse
    {
        public string? attendeeid { get; set; }
        [JsonProperty("preload-recordid")]
        public string? preloadrecordid { get; set; }
        public string? registrationstatus { get; set; }
        public string? testregistration { get; set; }
        public Category category { get; set; }
        public string? individualcost { get; set; }
        public string? cost { get; set; }
        public string? totalcost { get; set; }
        public string? received { get; set; }
        public string? recordid { get; set; }
        public List<Response> responses { get; set; }
        public string? created { get; set; }
        public string? tax { get; set; }
        public string? statetax { get; set; }
        public string? categorycost { get; set; }
        public string? subcategorycost { get; set; }
        public string? agendacost { get; set; }
        public string? optioncost { get; set; }
        public string? hotelcost { get; set; }
        public string? guestcost { get; set; }
        public string? adjustments { get; set; }
        public string? lastmodified { get; set; }
        public string? virtual_event_attendance { get; set; }
        public string? last_lobby_login { get; set; }
        public string? balancedue { get; set; }
    }

    public class Category
    {
        public string? categoryid { get; set; }
        public string? name { get; set; }
    }

    public class Response
    {
        public string? questionid { get; set; }
        public string? fieldname { get; set; }
        public string? name { get; set; }
        public string? pageid { get; set; }
        public string? page { get; set; }
        public string? choicekey { get; set; }
        public string? response { get; set; }
        public string? auto_capitalize { get; set; }
        public string? recording_views { get; set; }
    }

    
}
