namespace Ummanu.DummyAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Ummanu.DummyAPI.Data.Repositories;
using Ummanu.DummyAPI.Domain;
using Ummanu.DummyAPI.DTOs;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(IAppointmentRepository appointmentRepository, ILogger<AppointmentsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var existing = await appointmentRepository.GetByDispositionIdAsync(request.DispositionId);
        if (existing != null)
        {
            logger.LogInformation("Appointment already exists for disposition {DispositionId}", request.DispositionId);
            return Ok(AppointmentResponse.FromEntity(existing));
        }

        var position = await appointmentRepository.GetNextQueuePositionAsync(request.Urgency);
        var now = DateTime.UtcNow;

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            DispositionId = request.DispositionId,
            NhsNumber = request.NhsNumber,
            PatientName = request.PatientName,
            DateOfBirth = request.DateOfBirth,
            DxCode = request.DxCode,
            DosCode = request.DosCode,
            Urgency = request.Urgency,
            Status = AppointmentStatus.Queued,
            WaitingRoomPosition = position,
            ReceivedAt = now,
            UpdatedAt = now,
            Notes = "Appointment created in Ummanu queue"
        };

        await appointmentRepository.AddAsync(appointment);
        logger.LogInformation("Created appointment {Id} for disposition {DispositionId} at position {Position}",
            appointment.Id, appointment.DispositionId, appointment.WaitingRoomPosition);

        return Ok(AppointmentResponse.FromEntity(appointment));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var appointment = await appointmentRepository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with id {id} not found." });
        }

        return Ok(AppointmentResponse.FromEntity(appointment));
    }

    [HttpGet("disposition/{dispositionId:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDispositionId(Guid dispositionId)
    {
        var appointment = await appointmentRepository.GetByDispositionIdAsync(dispositionId);
        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with disposition id {dispositionId} not found." });
        }

        return Ok(AppointmentResponse.FromEntity(appointment));
    }

    [HttpGet("queue")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue()
    {
        var queue = await appointmentRepository.GetQueueAsync();
        return Ok(queue.Select(AppointmentResponse.FromEntity));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusRequest request)
    {
        var appointment = await appointmentRepository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with id {id} not found." });
        }

        if (!Enum.TryParse<AppointmentStatus>(request.Status, true, out var status))
        {
            return BadRequest(new { message = $"Invalid status '{request.Status}'." });
        }

        appointment.Status = status;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            appointment.Notes = request.Notes;
        }

        appointment.UpdatedAt = DateTime.UtcNow;
        await appointmentRepository.UpdateAsync(appointment);

        return Ok(AppointmentResponse.FromEntity(appointment));
    }

    [HttpPut("{id:guid}/assign-clinician")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignClinician(Guid id, [FromBody] AssignClinicianRequest request)
    {
        var appointment = await appointmentRepository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with id {id} not found." });
        }

        appointment.AssignedClinicianId = request.ClinicianId;
        appointment.Status = AppointmentStatus.InConsultation;
        appointment.UpdatedAt = DateTime.UtcNow;
        await appointmentRepository.UpdateAsync(appointment);

        logger.LogInformation("Assigned clinician {ClinicianId} to appointment {Id}, status moved to InConsultation",
            request.ClinicianId, id);

        return Ok(AppointmentResponse.FromEntity(appointment));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var appointment = await appointmentRepository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with id {id} not found." });
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTime.UtcNow;
        await appointmentRepository.UpdateAsync(appointment);

        logger.LogInformation("Cancelled appointment {Id}", id);

        return Ok(AppointmentResponse.FromEntity(appointment));
    }
}

