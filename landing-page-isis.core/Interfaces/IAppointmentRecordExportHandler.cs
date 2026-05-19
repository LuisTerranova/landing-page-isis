namespace landing_page_isis.core.Interfaces;

public interface IAppointmentRecordExportHandler
{
    Task<byte[]> ExportPatientRecords(Guid patientId, string format);
}
