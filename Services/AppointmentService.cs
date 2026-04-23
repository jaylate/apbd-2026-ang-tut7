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

        await connection.OpenAsync();
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

    public async Task<AppointmentDetailsDto?> GetById(int idAppointment)
    {
        using var connection = new SqlConnection(_connectionString);

        string query = """
			SELECT
			a.IdAppointment,
			a.AppointmentDate,
			a.Status,
			a.Reason,
			a.InternalNotes,
			a.CreatedAt,
			p.FirstName + N' ' + p.LastName AS PatientFullName,
			p.Email AS PatientEmail,
			p.PhoneNumber AS PatientPhone,
			d.LicenseNumber AS DoctorLicenseNumber
			FROM dbo.Appointments a
			JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
			JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
			WHERE a.IdAppointment = @IdAppointment;
		""";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdAppointment", idAppointment);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ?
            new AppointmentDetailsDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
                RecordCreationDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                PatientFullName = reader.GetString(6),
                PatientEmail = reader.GetString(7),
                PatientPhone = reader.IsDBNull(8) ? null : reader.GetString(8),
                DoctorLicenseNumber = reader.IsDBNull(9) ? null : reader.GetString(9)
            }
            : null;
    }

    public async Task<int> Create(CreateAppointmentRequestDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string checkPatient = """
            SELECT COUNT(1) FROM dbo.Patients
            WHERE IdPatient = @IdPatient AND IsActive = 1;
        """;
        using (var command = new SqlCommand(checkPatient, connection))
        {
            command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
            var patientExists = (int)(await command.ExecuteScalarAsync() ?? 0);
            if (patientExists == 0)
            {
                throw new ArgumentException("Patient does not exist or is not active.");
            }
        }

        string checkDoctor = """
            SELECT COUNT(1) FROM dbo.Doctors
            WHERE IdDoctor = @IdDoctor AND IsActive = 1;
        """;
        using (var command = new SqlCommand(checkDoctor, connection))
        {
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            var doctorExists = (int)(await command.ExecuteScalarAsync() ?? 0);
            if (doctorExists == 0)
            {
                throw new ArgumentException("Doctor does not exist or is not active.");
            }
        }

        string checkConflict = """
            SELECT COUNT(1) FROM dbo.Appointments
            WHERE IdDoctor = @IdDoctor
            AND AppointmentDate = @AppointmentDate
            AND Status = 'Scheduled';
        """;
        using (var command = new SqlCommand(checkConflict, connection))
        {
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
            var conflictExists = (int)(await command.ExecuteScalarAsync() ?? 0);
            if (conflictExists > 0)
            {
                throw new InvalidOperationException("Doctor already has a scheduled appointment at this time.");
            }
        }

        string insertQuery = """
            INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
            VALUES (@IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
        """;
        using (var command = new SqlCommand(insertQuery, connection))
        {
            command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
            command.Parameters.AddWithValue("@Reason", dto.Reason);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }

    public async Task Update(int id, UpdateAppointmentRequestDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string getAppointment = """
            SELECT IdAppointment, Status, AppointmentDate FROM dbo.Appointments
            WHERE IdAppointment = @IdAppointment;
        """;
        string? currentStatus = null;
        DateTime? currentDate = null;
        using (var command = new SqlCommand(getAppointment, connection))
        {
            command.Parameters.AddWithValue("@IdAppointment", id);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new KeyNotFoundException("Appointment not found.");
            }
            currentStatus = reader.GetString(1);
            currentDate = reader.GetDateTime(2);
        }

        if (currentStatus == "Completed" && dto.AppointmentDate != currentDate)
        {
            throw new InvalidOperationException("Cannot change date of a completed appointment.");
        }

        string checkPatient = """
            SELECT COUNT(1) FROM dbo.Patients
            WHERE IdPatient = @IdPatient AND IsActive = 1;
        """;
        using (var command = new SqlCommand(checkPatient, connection))
        {
            command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
            var patientExists = (int)(await command.ExecuteScalarAsync() ?? 0);
            if (patientExists == 0)
            {
                throw new ArgumentException("Patient does not exist or is not active.");
            }
        }

        string checkDoctor = """
            SELECT COUNT(1) FROM dbo.Doctors
            WHERE IdDoctor = @IdDoctor AND IsActive = 1;
        """;
        using (var command = new SqlCommand(checkDoctor, connection))
        {
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            var doctorExists = (int)(await command.ExecuteScalarAsync() ?? 0);
            if (doctorExists == 0)
            {
                throw new ArgumentException("Doctor does not exist or is not active.");
            }
        }

        var validStatuses = new[] { "Scheduled", "Completed", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
        {
            throw new ArgumentException("Status must be one of: Scheduled, Completed, Cancelled.");
        }

        if (dto.AppointmentDate != currentDate)
        {
            string checkConflict = """
                SELECT COUNT(1) FROM dbo.Appointments
                WHERE IdDoctor = @IdDoctor
                AND AppointmentDate = @AppointmentDate
                AND Status = 'Scheduled'
                AND IdAppointment != @IdAppointment;
            """;
            using (var command = new SqlCommand(checkConflict, connection))
            {
                command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
                command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
                command.Parameters.AddWithValue("@IdAppointment", id);
                var conflictExists = (int)(await command.ExecuteScalarAsync() ?? 0);
                if (conflictExists > 0)
                {
                    throw new InvalidOperationException("Doctor already has a scheduled appointment at this time.");
                }
            }
        }

        string updateQuery = """
            UPDATE dbo.Appointments
            SET IdPatient = @IdPatient,
                IdDoctor = @IdDoctor,
                AppointmentDate = @AppointmentDate,
                Status = @Status,
                Reason = @Reason,
                InternalNotes = @InternalNotes
            WHERE IdAppointment = @IdAppointment;
        """;
        using (var command = new SqlCommand(updateQuery, connection))
        {
            command.Parameters.AddWithValue("@IdAppointment", id);
            command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
            command.Parameters.AddWithValue("@Status", dto.Status);
            command.Parameters.AddWithValue("@Reason", dto.Reason);
            command.Parameters.AddWithValue("@InternalNotes", (object?)dto.InternalNotes ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task Delete(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string getAppointment = """
            SELECT Status FROM dbo.Appointments
            WHERE IdAppointment = @IdAppointment;
        """;
        using (var command = new SqlCommand(getAppointment, connection))
        {
            command.Parameters.AddWithValue("@IdAppointment", id);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new KeyNotFoundException("Appointment not found.");
            }
            var status = reader.GetString(0);
            if (status == "Completed")
            {
                throw new InvalidOperationException("Cannot delete a completed appointment.");
            }
        }

        string deleteQuery = """
            DELETE FROM dbo.Appointments
            WHERE IdAppointment = @IdAppointment;
        """;
        using (var command = new SqlCommand(deleteQuery, connection))
        {
            command.Parameters.AddWithValue("@IdAppointment", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
