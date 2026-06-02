using System;
using System.Collections.Generic;

namespace SystemStacjiNarciarskiejDLL.Models.DTOs.Admin
{
    public class SalesChartDto
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> OnlineValues { get; set; } = new List<decimal>();
        public List<decimal> OnsiteValues { get; set; } = new List<decimal>();
    }
}
