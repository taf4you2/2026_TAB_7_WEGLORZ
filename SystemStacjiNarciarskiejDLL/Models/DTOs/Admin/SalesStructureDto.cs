using System.Collections.Generic;

namespace SystemStacjiNarciarskiejDLL.Models.DTOs.Admin
{
    public class SalesStructureDto
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Values { get; set; } = new List<decimal>();
    }
}
