using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Cma.Repository.Model.Aventri
{
    [Table("AttendeeEvent")]
    public class AttendeeEvent
    {
        public int Id { get; set; }
        public string? EventId { get; set; }
        public int ProcessingStatus { get; set; } //indicates the current state of the record in the processing pipeline
        public bool IsProcessed { get; set; } //indicates whether the record has been fully processed and can be archived or deleted
        public string? Payload { get; set; } //contains the serialized input request to the webhook
        public DateTime? CreatedOn { get; set; } //Date record was created, used for tracking and purging old records
        public DateTime? UpdatedOn { get; set; } //Date record was last updated, used for tracking and purging old records
    }
}
