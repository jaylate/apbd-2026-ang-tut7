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
}
