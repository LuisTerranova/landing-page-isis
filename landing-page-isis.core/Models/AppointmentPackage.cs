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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Pacient? Pacient { get; set; }
}