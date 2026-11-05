namespace NHS111.UnitTests;

using System.Net;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NHS111.Contracts.Events;
using NHS111.Infrastructure.Http;
using NHS111.UmmanuConsumer.Consumers;
using Xunit;

public class UmmanuDispositionConsumerTests
{
    private readonly Mock<ISendEndpointProvider> _sendEndpointProviderMock = new();
    private readonly Mock<ILogger<UmmanuDispositionConsumer>> _loggerMock = new();

    private static (UmmanuHttpClient Client, Mock<HttpMessageHandler> HandlerMock) CreateMockHttpClient(HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:7001/")
        };

        return (new UmmanuHttpClient(httpClient), handlerMock);
    }

    [Theory]
    [InlineData("Adastra")]
    [InlineData("Unknown")]
    [InlineData("")]
    public async Task Consume_WhenRoutedToIsNotUmmanuOrBoth_ShouldSkip(string routedTo)
    {
        // Arrange
        var (client, handlerMock) = CreateMockHttpClient(HttpStatusCode.OK);
        var consumer = new UmmanuDispositionConsumer(client, _sendEndpointProviderMock.Object, _loggerMock.Object);

        var message = new DispositionCreatedEvent(
            Guid.NewGuid(), "1234567890", "John Doe", DateTime.UtcNow.AddYears(-30),
            "Dx001", "Dos1", "Routine", routedTo, DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<DispositionCreatedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        contextMock.Verify(
            x => x.Publish(It.IsAny<DispositionStatusUpdatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Theory]
    [InlineData("Ummanu")]
    [InlineData("Both")]
    public async Task Consume_WhenSendAppointmentSucceeds_ShouldPublishInProgressStatus(string routedTo)
    {
        // Arrange
        var (client, _) = CreateMockHttpClient(HttpStatusCode.OK);
        var consumer = new UmmanuDispositionConsumer(client, _sendEndpointProviderMock.Object, _loggerMock.Object);

        var dispositionId = Guid.NewGuid();
        var message = new DispositionCreatedEvent(
            dispositionId, "1234567890", "John Doe", DateTime.UtcNow.AddYears(-30),
            "Dx001", "Dos1", "Routine", routedTo, DateTime.UtcNow);

        DispositionStatusUpdatedEvent? publishedEvent = null;
        var contextMock = new Mock<ConsumeContext<DispositionCreatedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        contextMock
            .Setup(x => x.Publish(It.IsAny<DispositionStatusUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DispositionStatusUpdatedEvent, CancellationToken>((ev, _) => publishedEvent = ev)
            .Returns(Task.CompletedTask);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.DispositionId.Should().Be(dispositionId);
        publishedEvent.NewStatus.Should().Be("InProgress");
        publishedEvent.UpdatedBy.Should().Be("Ummanu");
        publishedEvent.Notes.Should().Be("Appointment created in Ummanu queue");
    }

    [Fact]
    public async Task Consume_WhenSendAppointmentFails_ShouldPublishFailedStatus()
    {
        // Arrange
        var (client, _) = CreateMockHttpClient(HttpStatusCode.InternalServerError);
        var consumer = new UmmanuDispositionConsumer(client, _sendEndpointProviderMock.Object, _loggerMock.Object);

        var dispositionId = Guid.NewGuid();
        var message = new DispositionCreatedEvent(
            dispositionId, "1234567890", "John Doe", DateTime.UtcNow.AddYears(-30),
            "Dx001", "Dos1", "Routine", "Ummanu", DateTime.UtcNow);

        DispositionStatusUpdatedEvent? publishedEvent = null;
        var contextMock = new Mock<ConsumeContext<DispositionCreatedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        contextMock
            .Setup(x => x.Publish(It.IsAny<DispositionStatusUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DispositionStatusUpdatedEvent, CancellationToken>((ev, _) => publishedEvent = ev)
            .Returns(Task.CompletedTask);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.DispositionId.Should().Be(dispositionId);
        publishedEvent.NewStatus.Should().Be("Failed");
        publishedEvent.UpdatedBy.Should().Be("Ummanu");
        publishedEvent.Notes.Should().Be("Failed to create appointment in Ummanu after retries");
    }
}

