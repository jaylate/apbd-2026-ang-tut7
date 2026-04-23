using apbd_2026_ang_tut7.DTOs;
using Microsoft.Data.SqlClient;

namespace apbd_2026_ang_tut7.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Missing ConnectionString 'DefaultConnection' in configuration!");
    }

    public async Task<IEnumerable<AppointmentListDto>> GetAll(
            string? status,
            string? patientLastName
            )
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();


        string query = """
			SELECT
			a.IdAppointment,
			a.AppointmentDate,
			a.Status,
			a.Reason,
			p.FirstName + N' ' + p.LastName AS PatientFullName,
			p.Email AS PatientEmail
			FROM dbo.Appointments a
			JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
			WHERE (@Status IS NULL OR a.Status = @Status)
			AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
			ORDER BY a.AppointmentDate;
		""";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();

        var results = new List<AppointmentListDto>();

        while (await reader.ReadAsync())
        {
            results.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });
        }

        return results;
    }
}
