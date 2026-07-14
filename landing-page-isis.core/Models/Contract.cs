using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

/// <summary>
/// Represents a service contract drafted for a patient or couple, tracking pricing, terms acceptance, and signature metadata.
/// </summary>
public class Contract
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ContractParticipantInfo PrimaryPatient { get; set; } = new();

    public bool TermsAccepted { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Rascunho;

    public decimal? Price { get; set; }

    public int? InitialAppointments { get; set; }

    public decimal? PackagePrice { get; set; }

    public string? AcceptanceToken { get; set; }

    public DateTimeOffset? TokenGeneratedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public string? ContractDocumentHtml { get; set; }

    public Guid? PackageId { get; set; }

    public AppointmentPackage? Package { get; set; }

    public Guid? PatientId { get; set; }


    public Patient? Patient { get; set; }

    public Guid? CoupleId { get; set; }

    public Couple? Couple { get; set; }

    public string? CoupleName { get; set; }

    public ContractParticipantInfo? SecondaryPatient { get; set; }
}
