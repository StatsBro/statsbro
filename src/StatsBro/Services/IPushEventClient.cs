using StatsBro.Domain.Models;

namespace StatsBro.Services;

public interface IPushEventClient
{
    Task PushAsync(EventPayload payload);
}
