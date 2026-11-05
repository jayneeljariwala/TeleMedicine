namespace NHS111.UnitTests;

using System.Net;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NHS111.Contracts.Events;
using NHS111.Infrastructure.Http;
using NHS111.AdastraConsumer.Consumers;
using Xunit;

public class AdastraStatusConsumerTests
{
    private readonly Mock<ILogger<AdastraStatusConsumer>> _loggerMock = new();

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
    [InlineData("Adastra")]
    [InlineData("System")]
    [InlineData("")]
    public async Task Consume_WhenUpdatedByIsNotUmmanu_ShouldSkip(string updatedBy)
    {
        // Arrange
        var (client, handlerMock) = CreateMockHttpClient(HttpStatusCode.OK);
        var consumer = new AdastraStatusConsumer(client, _loggerMock.Object);

        var message = new DispositionStatusUpdatedEvent(
            Guid.NewGuid(), "InProgress", updatedBy, "Notes", DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<DispositionStatusUpdatedEvent>>();
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
    }

    [Fact]
    public async Task Consume_WhenUpdatedByIsUmmanu_ShouldCallResolveReferral()
    {
        // Arrange
        var (client, handlerMock) = CreateMockHttpClient(HttpStatusCode.OK);
        var consumer = new AdastraStatusConsumer(client, _loggerMock.Object);

        var dispositionId = Guid.NewGuid();
        var message = new DispositionStatusUpdatedEvent(
            dispositionId, "InProgress", "Ummanu", "Appointment created in Ummanu queue", DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<DispositionStatusUpdatedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains($"api/referrals/{dispositionId}/resolve")),
            ItExpr.IsAny<CancellationToken>());
    }
}

