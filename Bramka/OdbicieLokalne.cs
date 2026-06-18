using System;

namespace Bramka
{
    public class OdbicieLokalne
    {
        public int Id { get; set; }
        public string CardId { get; set; } = string.Empty;
        public int GateId { get; set; }
        public DateTime ScanTime { get; set; }
        public DateTime? TimeBlockedUntil { get; set; }
        public int VerificationResultId { get; set; }
        public int? PassTypeId { get; set; }
    }
}
