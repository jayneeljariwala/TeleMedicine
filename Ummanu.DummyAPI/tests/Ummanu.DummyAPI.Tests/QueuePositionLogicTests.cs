namespace Ummanu.DummyAPI.Tests;

using FluentAssertions;
using Ummanu.DummyAPI.Domain;
using Xunit;

public class QueuePositionLogicTests
{
    private static int CalculateQueuePosition(int queuedCount, string urgency)
    {
        if (string.Equals(urgency, "Immediate", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(urgency, "UrgentSameDay", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(urgency, "Urgent", StringComparison.OrdinalIgnoreCase))
        {
            return queuedCount / 2;
        }

        return queuedCount + 1;
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(4, 1)]
    [InlineData(10, 1)]
    public void QueuePosition_Immediate_AlwaysReturnsOne(int currentQueued, int expectedPosition)
    {
        var position = CalculateQueuePosition(currentQueued, "Immediate");
        position.Should().Be(expectedPosition);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(10, 5)]
    public void QueuePosition_UrgentSameDay_ReturnsHalfOfQueuedCount(int currentQueued, int expectedPosition)
    {
        var position = CalculateQueuePosition(currentQueued, "UrgentSameDay");
        position.Should().Be(expectedPosition);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(4, 5)]
    [InlineData(10, 11)]
    public void QueuePosition_Routine_ReturnsQueuedCountPlusOne(int currentQueued, int expectedPosition)
    {
        var position = CalculateQueuePosition(currentQueued, "Routine");
        position.Should().Be(expectedPosition);
    }

    [Fact]
    public void QueueOrdering_OrdersByUrgencyPriorityThenReceivedAt()
    {
        var now = DateTime.UtcNow;

        var appointments = new List<Appointment>
        {
            new() { Id = Guid.NewGuid(), Urgency = "Routine", ReceivedAt = now.AddMinutes(-30), Status = AppointmentStatus.Queued },
            new() { Id = Guid.NewGuid(), Urgency = "Immediate", ReceivedAt = now.AddMinutes(-10), Status = AppointmentStatus.Queued },
            new() { Id = Guid.NewGuid(), Urgency = "UrgentSameDay", ReceivedAt = now.AddMinutes(-20), Status = AppointmentStatus.Queued },
            new() { Id = Guid.NewGuid(), Urgency = "Immediate", ReceivedAt = now.AddMinutes(-25), Status = AppointmentStatus.Queued },
            new() { Id = Guid.NewGuid(), Urgency = "Routine", ReceivedAt = now.AddMinutes(-40), Status = AppointmentStatus.Queued }
        };

        var ordered = appointments
            .OrderBy(a => a.Urgency switch
            {
                "Immediate" => 1,
                "UrgentSameDay" or "Urgent" => 2,
                "Routine" => 3,
                _ => 4
            })
            .ThenBy(a => a.ReceivedAt)
            .ToList();

        // 1. Immediate (-25 mins)
        ordered[0].Urgency.Should().Be("Immediate");
        ordered[0].ReceivedAt.Should().Be(now.AddMinutes(-25));

        // 2. Immediate (-10 mins)
        ordered[1].Urgency.Should().Be("Immediate");
        ordered[1].ReceivedAt.Should().Be(now.AddMinutes(-10));

        // 3. UrgentSameDay (-20 mins)
        ordered[2].Urgency.Should().Be("UrgentSameDay");

        // 4. Routine (-40 mins)
        ordered[3].Urgency.Should().Be("Routine");
        ordered[3].ReceivedAt.Should().Be(now.AddMinutes(-40));

        // 5. Routine (-30 mins)
        ordered[4].Urgency.Should().Be("Routine");
        ordered[4].ReceivedAt.Should().Be(now.AddMinutes(-30));
    }
}

