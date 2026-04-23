using Microsoft.AspNetCore.Mvc;
using apbd_2026_ang_tut7.Services;
using apbd_2026_ang_tut7.DTOs;

namespace apbd_2026_ang_tut7.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AppointmentsController(IAppointmentService service)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(string? status, string? patientLastName)
    {
        var appointments = await service.GetAll(status, patientLastName);
        return Ok(appointments);
    }

    [HttpGet("{idAppointment:int}")]
    public async Task<IActionResult> GetById([FromRoute] int idAppointment)
    {
        var appointment = await service.GetById(idAppointment);

        return appointment is null
            ? NotFound("No such appointment")
            : Ok(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequestDto dto)
    {
        if (dto.AppointmentDate < DateTime.Now)
        {
            return BadRequest("Appointment date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest("Reason for appointment is required.");
        }

        try
        {
            int idAppointment = await service.Create(dto);
            return CreatedAtAction(nameof(GetById), new { idAppointment }, null);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}