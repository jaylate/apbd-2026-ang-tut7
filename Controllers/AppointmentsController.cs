using Microsoft.AspNetCore.Mvc;
using apbd_2026_ang_tut7.Services;

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
}
