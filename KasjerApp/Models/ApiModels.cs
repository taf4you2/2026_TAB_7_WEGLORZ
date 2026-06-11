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

// ── Użytkownicy ───────────────────────────────────────────────────────────────
public record UserDto(int Id, string Email);
public record CreateUserRequest(string Email);

// ── Żądania ───────────────────────────────────────────────────────────────────
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

public record BlockCardRequest(string Reason);
public record ReturnPassRequest(string Reason, bool ReturnCard);
public record IssueCardRequest(string Id);
