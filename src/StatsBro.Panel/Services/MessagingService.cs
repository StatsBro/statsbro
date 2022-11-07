using StatsBro.Domain.Models;

namespace StatsBro.Panel.Services;

public interface IMessagingService
{
    Task<string> NewUserRegistration(User user);
}
public class MessagingService : IMessagingService
{
    public async Task<string> NewUserRegistration(User user)
    {
        // TODO add loging around this, like welcome email, activation email
        // slack notification that we have a new registration
        await Task.Delay(100);

        return "1234";
    }
}
