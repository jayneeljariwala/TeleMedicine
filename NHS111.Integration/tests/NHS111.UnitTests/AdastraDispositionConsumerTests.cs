namespace NHS111.UnitTests;

using System.Net;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NHS111.Contracts.Events;
using NHS111.Infrastructure.Http;
using NHS111.AdastraConsumer.Consumers;
using Xunit;

public class AdastraDispositionConsumerTests
{
    private readonly Mock<ISendEndpointProvider> _sendEndpointProviderMock = new();
    private readonly Mock<ILogger<AdastraDispositionConsumer>> _loggerMock = new();

    private static (AdastraHttpClient Client, Mock<HttpMessageHandler> HandlerMock) CreateMockHttpClient(HttpStatusCode statusCode)
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
            BaseAddress = new Uri("http://localhost:7002/")
        };

        return (new AdastraHttpClient(httpClient), handlerMock);
    }

    [Theory]
    [InlineData("Ummanu")]
    [InlineData("Unknown")]
    [InlineData("")]
    public async Task Consume_WhenRoutedToIsNotAdastraOrBoth_ShouldSkip(string routedTo)
    {
        // Arrange
        var (client, handlerMock) = CreateMockHttpClient(HttpStatusCode.OK);
        var consumer = new AdastraDispositionConsumer(client, _sendEndpointProviderMock.Object, _loggerMock.Object);

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
    [InlineData("Adastra")]
    [InlineData("Both")]
    public async Task Consume_WhenSendReferralSucceeds_ShouldPublishCompletedStatus(string routedTo)
    {
        // Arrange
        var (client, _) = CreateMockHttpClient(HttpStatusCode.OK);
        var consumer = new AdastraDispositionConsumer(client, _sendEndpointProviderMock.Object, _loggerMock.Object);

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
        publishedEvent.NewStatus.Should().Be("Completed");
        publishedEvent.UpdatedBy.Should().Be("Adastra");
        publishedEvent.Notes.Should().Be("Referral created in Adastra");
    }

    [Fact]
    public async Task Consume_WhenSendReferralFails_ShouldNotPublishStatus()
    {
        // Arrange
        var (client, _) = CreateMockHttpClient(HttpStatusCode.InternalServerError);
        var consumer = new AdastraDispositionConsumer(client, _sendEndpointProviderMock.Object, _loggerMock.Object);

        var dispositionId = Guid.NewGuid();
        var message = new DispositionCreatedEvent(
            dispositionId, "1234567890", "John Doe", DateTime.UtcNow.AddYears(-30),
            "Dx001", "Dos1", "Routine", "Adastra", DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<DispositionCreatedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        contextMock.Verify(
            x => x.Publish(It.IsAny<DispositionStatusUpdatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}

