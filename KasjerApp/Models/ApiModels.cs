namespace KasjerApp.Models;

// ── Taryfy ──────────────────────────────────────────────────────────────────
public record TariffDto(int Id, string Name, string? Season, string? PassType, decimal? Price, int? RideCount, int? PoolLimit);

// ── Karty RFID ───────────────────────────────────────────────────────────────
public record CardDto(string Id, string Status, string? Owner, string? ActivePassType, int? ActivePassId, DateTime? ValidTo, bool DepositPaid, string? BlockReason);
public record CardIssueVerificationDto(bool CanIssue, string Message, CardDto? Card);
public record CardReturnDto(decimal DepositReturn);

// ── Karnety ──────────────────────────────────────────────────────────────────
public record PassDto(int Id, string CardId, string? Status, string? Tariff, string? PassType,
    DateTime? ValidFrom, DateTime? ValidTo, int? InitialRides, int? RemainingRides, string? BlockReason);

public record ReturnPreviewDto(decimal GrossAmount, int TotalDays, int UsedDays,
    decimal RefundForUnusedDays, decimal ManipulationFee, decimal DepositReturn, decimal TotalRefund);

// ── Transakcje ────────────────────────────────────────────────────────────────
public record TransactionDto(int Id, string? OperationType, string? Tariff, decimal Amount,
    DateTime? Date, string? CashierLogin);

public record ShiftReportDto(string CashierLogin, DateOnly Date, int TotalSalesCount,
    decimal TotalSalesAmount, int TotalReturnsCount, decimal TotalReturnsAmount,
    decimal NetRevenue, decimal CashAmount, decimal CardAmount);

// ── Statystyki ────────────────────────────────────────────────────────────────
// ── Raport zmiany ─────────────────────────────────────────────────────────────
// ── Przejazdy ─────────────────────────────────────────────────────────────────
public record GateScanDto(int Id, string? CardId, string? GateName, string? LiftName,
    DateTime? ScanTime, string? Result);

// ── Oczekujące zwroty ─────────────────────────────────────────────────────────
public record PendingReturnDto(int PassId, string CardRfid, string? OwnerEmail, string? PassType,
    DateTime? ValidTo, int RemainingDays, decimal EstimatedRefund);

// ── Użytkownicy ───────────────────────────────────────────────────────────────
public record UserDto(int Id, string Email);
public record CreateUserRequest(string Email);

// ── Żądania ───────────────────────────────────────────────────────────────────
public record SellTicketRequest(string CardId, int TariffId, DateTime ValidOn, int Quantity);
public record SellTicketResponse(int ReservationId, int Quantity, decimal TotalAmount, DateTime ValidOn);

public record CreatePassRequest(string CardId, int TariffId, DateTime ValidFrom, DateTime ValidTo, int? UserId);

public record BlockPassRequest(string Reason);
public record BlockCardRequest(string Reason);
public record ReturnPassRequest(string Reason, bool ReturnCard);
public record IssueCardRequest(string Id);
