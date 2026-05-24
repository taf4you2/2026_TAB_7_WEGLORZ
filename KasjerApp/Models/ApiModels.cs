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
    decimal RefundForUnusedDays, decimal ManipulationFee, decimal DepositReturn, decimal TotalRefund,
    bool CardReturnEligible, string? CardReturnBlockReason);

// ── Transakcje ────────────────────────────────────────────────────────────────
public record TransactionDto(int Id, string? OperationType, string? Tariff, decimal Amount,
    DateTime? Date, string? CashierLogin);

public record ShiftReportDto(string CashierLogin, DateOnly Date, int TotalSalesCount,
    decimal TotalSalesAmount, int TotalReturnsCount, decimal TotalReturnsAmount,
    decimal NetRevenue, decimal CashAmount, decimal CardAmount);

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
public record ReservationSearchDto(
    int Id,
    string ReservationNumber,
    DateTime? ReservationDate,
    string? Status,
    List<ReservationPassDto> Passes);

public record ReservationPassDto(
    int Id,
    string? CardId,
    string? Status,
    string? Tariff,
    decimal? Price,
    DateTime? ValidFrom,
    DateTime? ValidTo);

public record ActivatePassRequest(string ReservationNumber, string CardRFID, int? PassId = null);
public record ReservedPassActivationResponse(
    int ReservationId,
    string ReservationNumber,
    int PassId,
    string CardId,
    string? PassStatus,
    string? Tariff,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string? OwnerEmail);

public record BlockPassRequest(string Reason);
public record BlockCardRequest(string Reason);
public record ReturnPassRequest(string Reason, bool ReturnCard);
public record IssueCardRequest(string Id);
