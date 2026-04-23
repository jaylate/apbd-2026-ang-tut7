using apbd_2026_ang_tut7.DTOs;

namespace apbd_2026_ang_tut7.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAll(
            string? status,
            string? patientLastName
            );
    Task<AppointmentDetailsDto?> GetById(int id);
    Task<int> Create(CreateAppointmentRequestDto dto);
    Task Update(int id, UpdateAppointmentRequestDto dto);
}
