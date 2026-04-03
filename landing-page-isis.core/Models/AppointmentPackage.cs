namespace landing_page_isis.core.Models;

public class AppointmentPackage
{
    public Guid Id { get; set; }
    public Guid PacientId { get; set; }
    public int TotalAppointments { get; set; }
    public int RemainingAppointments { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Price { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Ativo;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Pacient? Pacient { get; set; }
}