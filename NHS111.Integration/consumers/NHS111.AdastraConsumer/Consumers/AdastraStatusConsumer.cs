namespace NHS111.AdastraConsumer.Consumers;

using MassTransit;
using Microsoft.Extensions.Logging;
using NHS111.Contracts.Events;
using NHS111.Infrastructure.Http;

public class AdastraStatusConsumer(
    AdastraHttpClient adastraHttpClient,
    ILogger<AdastraStatusConsumer> logger) : IConsumer<DispositionStatusUpdatedEvent>
{
    public async Task Consume(ConsumeContext<DispositionStatusUpdatedEvent> context)
    {
        var message = context.Message;

        // 1. Only process if message.UpdatedBy == "Ummanu"
        if (!string.Equals(message.UpdatedBy, "Ummanu", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 2. Log: "Ummanu handled {DispositionId} — resolving in Adastra"
        logger.LogInformation("Ummanu handled {DispositionId} — resolving in Adastra", message.DispositionId);

        // 3. Call AdastraHttpClient.ResolveReferralAsync(message.DispositionId, "Ummanu", ct)
        var success = await adastraHttpClient.ResolveReferralAsync(message.DispositionId, "Ummanu", context.CancellationToken);

        // 4. Log result
        if (success)
        {
            logger.LogInformation("Successfully resolved referral for disposition {DispositionId} in Adastra", message.DispositionId);
        }
        else
        {
            logger.LogWarning("Failed to resolve referral for disposition {DispositionId} in Adastra", message.DispositionId);
        }
    }
}

