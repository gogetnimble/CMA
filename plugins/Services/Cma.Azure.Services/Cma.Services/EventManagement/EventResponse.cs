using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cma.Services.EventManagement
{
    public class EventResponse
    {
        public string? eventid { get; set; }
        public string? accountid { get; set; }
        public string? name { get; set; }
        public string? code { get; set; }
        public string? city { get; set; }
        public string? startdate { get; set; }
        public string? enddate { get; set; }
        public string? timezoneid { get; set; }
        public string? dateformat { get; set; }
        public string? timeformat { get; set; }
        public string? currency_dec_point { get; set; }
        public string? currency_thousands_sep { get; set; }
        public string? status { get; set; }
        public Languages languages { get; set; }
        public string? defaultlanguage { get; set; }
        public string? createdby { get; set; }
        public string? deleted { get; set; }
        public string? eMobile { get; set; }
        public string? eReg { get; set; }
        public string? eSocial { get; set; }
        public string? calendar_country { get; set; }
        public string? country { get; set; }
        public string? modifiedby { get; set; }
        public string? locationname { get; set; }
        public string? state { get; set; }
        public string? eSelect { get; set; }
        public string? ipreoid { get; set; }
        public string? url { get; set; }
        public string? max_reg { get; set; }
        public Location location { get; set; }
        public string? starttime { get; set; }
        public string? endtime { get; set; }
        public string? closedate { get; set; }
        public string? closetime { get; set; }
        public string? homepage { get; set; }
        public Contactinfo? contactinfo { get; set; }
        public string? standardcurrency { get; set; }
        public string? line_item_tax { get; set; }
        public string? foldername { get; set; }
        public string? eventclosemessage { get; set; }
        public string? timezone { get; set; }
        public string? createddatetime { get; set; }
        public string? modifieddatetime { get; set; }
        public string? eventtype { get; set; }
    }

    public class Contactinfo
    {
        public string? eng { get; set; }
    }

    public class Languages
    {
        public string? eng { get; set; }
        public string? por { get; set; }
        public string? rus { get; set; }
        public string? spa { get; set; }
    }

    public class Location
    {
        public string? name { get; set; }
        public string? address1 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? postcode { get; set; }
        public string? country { get; set; }
        public string? phone { get; set; }
    }

    
}
