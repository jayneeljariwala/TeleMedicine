namespace Ummanu.DummyAPI.Tests;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Ummanu.DummyAPI.Controllers;
using Ummanu.DummyAPI.Data.Repositories;
using Ummanu.DummyAPI.Domain;
using Ummanu.DummyAPI.DTOs;
using Xunit;

public class AppointmentsControllerTests
{
    private readonly Mock<IAppointmentRepository> _repositoryMock = new();
    private readonly Mock<ILogger<AppointmentsController>> _loggerMock = new();
    private readonly AppointmentsController _controller;

    public AppointmentsControllerTests()
    {
        _controller = new AppointmentsController(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Post_CreateAppointment_Returns200WithAppointmentResponse()
    {
        // Arrange
        var request = new CreateAppointmentRequest(
            Guid.NewGuid(),
            "1234567890",
            "John Doe",
            new DateTime(1990, 1, 1),
            "Dx001",
            "Dos001",
            "Routine");

        _repositoryMock.Setup(r => r.GetByDispositionIdAsync(request.DispositionId))
            .ReturnsAsync((Appointment?)null);

        _repositoryMock.Setup(r => r.GetNextQueuePositionAsync(request.Urgency))
            .ReturnsAsync(3);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<AppointmentResponse>().Subject;
        response.DispositionId.Should().Be(request.DispositionId);
        response.PatientName.Should().Be("John Doe");
        response.Status.Should().Be("Queued");
        response.WaitingRoomPosition.Should().Be(3);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<Appointment>(a =>
            a.DispositionId == request.DispositionId &&
            a.Status == AppointmentStatus.Queued &&
            a.WaitingRoomPosition == 3)), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenExists_Returns200WithAppointment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = id,
            DispositionId = Guid.NewGuid(),
            PatientName = "Jane Doe",
            Status = AppointmentStatus.Queued,
            WaitingRoomPosition = 1,
            ReceivedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(appointment);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<AppointmentResponse>().Subject;
        response.Id.Should().Be(id);
        response.PatientName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((Appointment?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByDispositionId_WhenExists_Returns200()
    {
        // Arrange
        var dispositionId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            DispositionId = dispositionId,
            PatientName = "Alice",
            Status = AppointmentStatus.Queued,
            ReceivedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByDispositionIdAsync(dispositionId))
            .ReturnsAsync(appointment);

        // Act
        var result = await _controller.GetByDispositionId(dispositionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<AppointmentResponse>().Subject;
        response.DispositionId.Should().Be(dispositionId);
    }

    [Fact]
    public async Task GetQueue_ReturnsOrderedList()
    {
        // Arrange
        var appointments = new List<Appointment>
        {
            new() { Id = Guid.NewGuid(), Urgency = "Immediate", Status = AppointmentStatus.Queued, WaitingRoomPosition = 1, ReceivedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Urgency = "UrgentSameDay", Status = AppointmentStatus.Queued, WaitingRoomPosition = 2, ReceivedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Urgency = "Routine", Status = AppointmentStatus.Queued, WaitingRoomPosition = 3, ReceivedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetQueueAsync())
            .ReturnsAsync(appointments);

        // Act
        var result = await _controller.GetQueue();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<AppointmentResponse>>().Subject.ToList();
        response.Should().HaveCount(3);
        response[0].Urgency.Should().Be("Immediate");
        response[1].Urgency.Should().Be("UrgentSameDay");
        response[2].Urgency.Should().Be("Routine");
    }

    [Fact]
    public async Task AssignClinician_SetsAssignedClinicianIdAndInConsultation()
    {
        // Arrange
        var id = Guid.NewGuid();
        var clinicianId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = id,
            Status = AppointmentStatus.Queued,
            ReceivedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(appointment);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AssignClinician(id, new AssignClinicianRequest(clinicianId));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AppointmentResponse>().Subject;
        response.AssignedClinicianId.Should().Be(clinicianId);
        response.Status.Should().Be("InConsultation");

        appointment.AssignedClinicianId.Should().Be(clinicianId);
        appointment.Status.Should().Be(AppointmentStatus.InConsultation);
    }

    [Fact]
    public async Task Cancel_SetsStatusCancelled()
    {
        // Arrange
        var id = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = id,
            Status = AppointmentStatus.Queued,
            ReceivedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(appointment);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Cancel(id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AppointmentResponse>().Subject;
        response.Status.Should().Be("Cancelled");

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }
}

