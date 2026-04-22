namespace KasjerApp.Models;

public record LoginRequest(string Email, string Password, string Role);
public record LoginResponse(int UserId, string Role, string Token);
