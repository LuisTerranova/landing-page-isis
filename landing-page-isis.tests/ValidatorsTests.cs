using landing_page_isis.core.Models;
using landing_page_isis.core.Validators;
using Xunit;

namespace landing_page_isis.tests;

public class ValidatorsTests
{
    private readonly PatientValidator _patientValidator = new();
    private readonly AppointmentValidator _appointmentValidator = new();
    private readonly CoupleValidator _coupleValidator = new();
    private readonly LeadValidator _leadValidator = new();

    #region PatientValidator Tests

    [Fact]
    public void PatientValidator_ShouldPass_WhenPatientIsValid()
    {
        var patient = new Patient
        {
            Name = "Maria Silva",
            Email = "maria@exemplo.com",
            Phone = "11999998888",
            Cpf = "529.982.247-25",
        };

        var result = _patientValidator.Validate(patient);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PatientValidator_ShouldFail_WhenNameIsEmpty()
    {
        var patient = new Patient { Name = "" };

        var result = _patientValidator.Validate(patient);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Nome é obrigatório.");
    }

    [Fact]
    public void PatientValidator_ShouldFail_WhenEmailIsInvalid()
    {
        var patient = new Patient { Name = "João", Email = "email-invalido" };

        var result = _patientValidator.Validate(patient);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "E-mail inválido.");
    }

    [Fact]
    public void PatientValidator_ShouldFail_WhenCpfIsInvalid()
    {
        var patient = new Patient { Name = "João", Cpf = "111.111.111-11" };

        var result = _patientValidator.Validate(patient);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "CPF inválido.");
    }

    #endregion

    #region AppointmentValidator Tests

    [Fact]
    public void AppointmentValidator_ShouldFail_WhenNeitherPatientIdNorCoupleIdIsSet()
    {
        var appointment = new Appointment
        {
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            Price = 150m,
            PatientId = null,
            CoupleId = null,
        };

        var result = _appointmentValidator.Validate(appointment);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "O agendamento deve estar vinculado a um Paciente ou Casal.");
    }

    #endregion

    #region CoupleValidator Tests

    [Fact]
    public void CoupleValidator_ShouldFail_WhenNameIsEmpty()
    {
        var couple = new Couple
        {
            Name = "",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
        };

        var result = _coupleValidator.Validate(couple);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Nome do casal é obrigatório.");
    }

    [Fact]
    public void CoupleValidator_ShouldFail_WhenBothPartnersAreTheSame()
    {
        var patientId = Guid.NewGuid();
        var couple = new Couple
        {
            Name = "Casal Teste",
            Patient1Id = patientId,
            Patient2Id = patientId,
        };

        var result = _coupleValidator.Validate(couple);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Os dois pacientes devem ser diferentes.");
    }

    #endregion

    #region LeadValidator Tests

    [Fact]
    public void LeadValidator_ShouldPass_WhenLeadIsValid()
    {
        var lead = new Lead
        {
            Name = "Carlos Santos",
            Phone = "69999887766",
            Intent = "Terapia Individual",
        };

        var result = _leadValidator.Validate(lead);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void LeadValidator_ShouldFail_WhenNameIsEmpty()
    {
        var lead = new Lead
        {
            Name = "",
            Phone = "69999887766",
            Intent = "Terapia Individual",
        };

        var result = _leadValidator.Validate(lead);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Nome é obrigatório.");
    }

    [Fact]
    public void LeadValidator_ShouldFail_WhenPhoneIsInvalid()
    {
        var lead = new Lead
        {
            Name = "Carlos Santos",
            Phone = "123", // invalid length
            Intent = "Terapia Individual",
        };

        var result = _leadValidator.Validate(lead);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Telefone inválido. Deve ter 10 ou 11 dígitos.");
    }

    #endregion
}
