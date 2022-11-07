namespace StatsBro.Controllers;

using Microsoft.AspNetCore.Mvc;
using StatsBro.Domain.Models;
using StatsBro.Services;
using System.Text;

[Route("api")]
[ApiController]
public class StatsCatcherController : ControllerBase
{
    private readonly IPushEventClient _queueClient;
    private readonly ILogger<StatsCatcherController> _logger;

    public StatsCatcherController(
        IPushEventClient queueClient,
        ILogger<StatsCatcherController> logger
        )
    {
        this._queueClient = queueClient;
        this._logger = logger;
    }

    [HttpPost("s")]
    public async Task CatchStat()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(content))
            {
                var ip = ExtractCallerIpAddress(Request);

                // TODO: setup the size, or maybe it will be better to filter out on reverse-proxy
                if (content.Length < 4096)
                {
                    await _queueClient.PushAsync(new EventPayload { Content = content, IP = ip, Timestamp = DateTime.UtcNow });
                    Console.WriteLine(DateTime.Now);
                }
            }
        }catch (Exception ex)
        {
            this._logger.LogDebug("exception on catchStat: {message}", ex.Message);
        }
    }

    private static string ExtractCallerIpAddress(HttpRequest request)
    {
        var value = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if(!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var remoteIp = request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            return remoteIp.MapToIPv4().ToString();
        }

        return "0.0.0.0";
    }
}
