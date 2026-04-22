using System.ComponentModel.DataAnnotations;

namespace apbd_2026_ang_tut7.DTOs;

public class AppointmentDetailsDto
{
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
}