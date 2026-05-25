using Microsoft.AspNetCore.SignalR;

namespace SystemAPI.Hubs;

public class InfrastructureHub : Hub
{
    // Klienci będą odbierać powiadomienia, nie muszą nic wysyłać do serwera (na razie)
}
