namespace NHS111.UmmanuConsumer.Consumers;

using MassTransit;
using Microsoft.Extensions.Logging;
using NHS111.Contracts.Events;
using NHS111.Infrastructure.Http;

public class UmmanuDispositionConsumer(
    UmmanuHttpClient ummanuHttpClient,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<UmmanuDispositionConsumer> logger) : IConsumer<DispositionCreatedEvent>
{
    private readonly ISendEndpointProvider _sendEndpointProvider = sendEndpointProvider;

    public async Task Consume(ConsumeContext<DispositionCreatedEvent> context)
    {
        var message = context.Message;

        // 1. Skip if message.RoutedTo is not "Ummanu" or "Both"
        if (!string.Equals(message.RoutedTo, "Ummanu", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(message.RoutedTo, "Both", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 2. Log: "Processing disposition {DispositionId} for Ummanu"
        logger.LogInformation("Processing disposition {DispositionId} for Ummanu", message.DispositionId);

        // 3. Call UmmanuHttpClient.SendAppointmentAsync(context.Message, ct)
        var success = await ummanuHttpClient.SendAppointmentAsync(message, context.CancellationToken);

        // 4. On success (true):
        //      Publish DispositionStatusUpdatedEvent:
        //        NewStatus = "InProgress"
        //        UpdatedBy = "Ummanu"
        //        Notes = "Appointment created in Ummanu queue"
        //        UpdatedAt = DateTime.UtcNow
        //      Log: "Disposition {DispositionId} successfully sent to Ummanu"
        if (success)
        {
            await context.Publish(new DispositionStatusUpdatedEvent(
                message.DispositionId,
                "InProgress",
                "Ummanu",
                "Appointment created in Ummanu queue",
                DateTime.UtcNow), context.CancellationToken);

            logger.LogInformation("Disposition {DispositionId} successfully sent to Ummanu", message.DispositionId);
        }
        // 5. On failure (false) after retries:
        //      Publish DispositionStatusUpdatedEvent:
        //        NewStatus = "Failed"
        //        UpdatedBy = "Ummanu"
        //        Notes = "Failed to create appointment in Ummanu after retries"
        //      Log error
        else
        {
            await context.Publish(new DispositionStatusUpdatedEvent(
                message.DispositionId,
                "Failed",
                "Ummanu",
                "Failed to create appointment in Ummanu after retries",
                DateTime.UtcNow), context.CancellationToken);

            logger.LogError("Failed to create appointment in Ummanu after retries for disposition {DispositionId}", message.DispositionId);
        }
    }
}

