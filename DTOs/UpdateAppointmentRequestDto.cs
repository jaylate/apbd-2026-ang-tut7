using System.ComponentModel.DataAnnotations;

namespace apbd_2026_ang_tut7.DTOs;

public class UpdateAppointmentRequestDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int IdPatient { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int IdDoctor { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? InternalNotes { get; set; }
}
