namespace NHS111.AdastraConsumer.Consumers;

using MassTransit;
using Microsoft.Extensions.Logging;
using NHS111.Contracts.Events;
using NHS111.Infrastructure.Http;

public class AdastraDispositionConsumer(
    AdastraHttpClient adastraHttpClient,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<AdastraDispositionConsumer> logger) : IConsumer<DispositionCreatedEvent>
{
    private readonly ISendEndpointProvider _sendEndpointProvider = sendEndpointProvider;

    public async Task Consume(ConsumeContext<DispositionCreatedEvent> context)
    {
        var message = context.Message;

        // 1. Skip if message.RoutedTo is not "Adastra" or "Both"
        if (!string.Equals(message.RoutedTo, "Adastra", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(message.RoutedTo, "Both", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 2. Log: "Processing disposition {DispositionId} for Adastra"
        logger.LogInformation("Processing disposition {DispositionId} for Adastra", message.DispositionId);

        // 3. Call AdastraHttpClient.SendReferralAsync(context.Message, ct)
        var success = await adastraHttpClient.SendReferralAsync(message, context.CancellationToken);

        // 4. On success:
        //      Publish DispositionStatusUpdatedEvent:
        //        NewStatus = "Completed"
        //        UpdatedBy = "Adastra"
        //        Notes = "Referral created in Adastra"
        //      Log success
        if (success)
        {
            await context.Publish(new DispositionStatusUpdatedEvent(
                message.DispositionId,
                "Completed",
                "Adastra",
                "Referral created in Adastra",
                DateTime.UtcNow), context.CancellationToken);

            logger.LogInformation("Disposition {DispositionId} successfully sent to Adastra", message.DispositionId);
        }
        // 5. On failure after retries: log warning
        else
        {
            logger.LogWarning("Failed to send disposition {DispositionId} to Adastra after retries", message.DispositionId);
        }
    }
}

