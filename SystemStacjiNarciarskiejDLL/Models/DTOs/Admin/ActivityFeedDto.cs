using System;

namespace SystemStacjiNarciarskiejDLL.Models.DTOs.Admin
{
    public class ActivityFeedDto
    {
        public int Id { get; set; }
        public string CardRfid { get; set; }
        public string Location { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }
}
