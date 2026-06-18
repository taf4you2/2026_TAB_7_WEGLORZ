using System;

namespace SystemStacjiNarciarskiejDLL.Models.DTOs.Admin
{
    public class AdminReportHistoryDto
    {
        public int Id { get; set; }
        public string AdminLogin { get; set; }
        public string ReportTypeName { get; set; }
        public string Parameters { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
