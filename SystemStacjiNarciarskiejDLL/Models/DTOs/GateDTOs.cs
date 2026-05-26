using System;

namespace SystemStacjiNarciarskiejDLL.Models.DTOs;

public class GateScanRequestDto
{
    public string CardId { get; set; } = string.Empty;
    public int GateId { get; set; }
}

public class GateScanResponseDto
{
    public bool IsGranted { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
}

public class GateScanSyncDto
{
    public string? CardId { get; set; }
    public int? GateId { get; set; }
    public DateTime? ScanTime { get; set; }
    public DateTime? TimeBlockedUntil { get; set; }
    public int? VerificationResultId { get; set; }
    public int? PassTypeId { get; set; }
}
