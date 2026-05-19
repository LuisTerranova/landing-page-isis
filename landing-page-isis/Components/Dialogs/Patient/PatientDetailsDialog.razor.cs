using landing_page_isis.Components.Dialogs.Appointment;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using PatientModel = landing_page_isis.core.Models.Patient;

namespace landing_page_isis.Components.Dialogs.Patient;

public partial class PatientDetailsDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public PatientModel Patient { get; set; } = null!;

    [Inject]
    private IAppointmentRecordHandler RecordHandler { get; set; } = null!;

    [Inject]
    private IAppointmentPackageHandler PackageHandler { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IPatientHandler PatientHandler { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IAppointmentRecordExportHandler ExportHandler { get; set; } = null!;

    private bool _showCpf;
    private bool _isEditing;
    private bool _isFormValid;
    private PatientModel _model = new();
    private DateTime? _tempBirthDate;

    // Records
    private DateTime? _recordsFilterDate;
    private bool _isLoadingRecords;
    private bool _recordsLoaded;
    private List<AppointmentRecordListItemDto> _records = new();
    private int _recordPage;
    private int _totalRecords;
    private const int RecordsPageSize = 5;

    // Export
    private string _exportFormat = "pdf";
    private bool _isExporting;

    // Packages
    private bool _isLoadingPackages;
    private bool _packagesLoaded;
    private List<AppointmentPackageListItemDto> _packages = new();

    protected override void OnParametersSet()
    {
        ResetModel();
    }

    private void ResetModel()
    {
        _model = new PatientModel
        {
            Id = Patient.Id,
            Name = Patient.Name,
            Email = Patient.Email,
            Phone = Patient.Phone,
            Cpf = Patient.Cpf,
            BirthDate = Patient.BirthDate,
            StateOfResidency = Patient.StateOfResidency,
        };
        _tempBirthDate = _model.BirthDate?.ToDateTime(TimeOnly.MinValue);
    }

    private void ToggleCpfObfuscation() => _showCpf = !_showCpf;

    private void ToggleEdit()
    {
        if (!_isEditing)
        {
            _isEditing = true;
        }
        else
        {
            ResetModel();
            _isEditing = false;
        }
    }

    private async Task SavePatient()
    {
        if (!_isFormValid)
            return;

        _model.BirthDate = _tempBirthDate.HasValue
            ? DateOnly.FromDateTime(_tempBirthDate.Value)
            : null;

        var result = await PatientHandler.UpdatePatient(_model);
        if (result.Success)
        {
            Snackbar.Add("Dados do paciente atualizados com sucesso!", Severity.Success);

            // Reflects changes on the parent object
            Patient.Name = _model.Name;
            Patient.Email = _model.Email;
            Patient.Phone = _model.Phone;
            Patient.Cpf = _model.Cpf;
            Patient.BirthDate = _model.BirthDate;
            Patient.StateOfResidency = _model.StateOfResidency;

            _isEditing = false;
        }
        else
        {
            Snackbar.Add($"Erro ao atualizar paciente: {result.Message}", Severity.Error);
        }
    }

    private string GetDisplayCpf()
    {
        if (string.IsNullOrWhiteSpace(_model.Cpf))
            return "Não informado";
        if (_showCpf)
            return _model.Cpf.FormatCpf();

        var nums = new string(_model.Cpf.Where(char.IsDigit).ToArray());
        if (nums.Length < 3)
            return "***";
        var last2 = nums.Substring(nums.Length - 2);
        return $"***.***.***-{last2}";
    }

    private string GetCpfIcon() =>
        _showCpf ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;

    private async Task OnTabChanged(int index)
    {
        if (index == 1 && !_recordsLoaded)
        {
            await LoadRecordsAsync();
        }
        else if (index == 2 && !_packagesLoaded)
        {
            await LoadPackagesAsync();
        }
    }

    private async Task OnFilterDateChanged(DateTime? newDate)
    {
        _recordsFilterDate = newDate;
        _records.Clear();
        _recordPage = 0;
        await LoadRecordsAsync();
    }

    private async Task LoadRecordsAsync()
    {
        _isLoadingRecords = true;
        StateHasChanged();

        var result = await RecordHandler.GetRecordsByPatientId(
            _recordPage,
            RecordsPageSize,
            Patient.Id,
            _recordsFilterDate,
            CancellationToken.None
        );

        _records.AddRange(result.Items);
        _totalRecords = result.TotalItems;
        _recordsLoaded = true;
        _isLoadingRecords = false;
    }

    private async Task LoadMoreRecords()
    {
        _recordPage++;
        await LoadRecordsAsync();
    }

    private async Task LoadPackagesAsync()
    {
        _isLoadingPackages = true;
        StateHasChanged();

        var result = await PackageHandler.GetPackagesByPatientId(
            0,
            50,
            Patient.Id,
            CancellationToken.None
        );
        _packages = result.Items.ToList();

        _packagesLoaded = true;
        _isLoadingPackages = false;
    }

    private async Task OpenCreatePackage()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<AppointmentPackageDialog>(string.Empty, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: AppointmentPackage newPackage })
        {
            newPackage.PatientId = Patient.Id;
            var response = await PackageHandler.CreatePackage(newPackage);

            if (response.Success)
            {
                Snackbar.Add("Pacote criado com sucesso!", Severity.Success);
                _packagesLoaded = false;
                await LoadPackagesAsync();
            }
            else
            {
                Snackbar.Add($"Erro ao criar pacote: {response.Message}", Severity.Warning);
            }
        }
    }

    private async Task OpenEditPackage(AppointmentPackageListItemDto pkg)
    {
        var parameters = new DialogParameters<AppointmentPackageDialog>
        {
            {
                x => x.Model,
                new AppointmentPackage
                {
                    Id = pkg.Id,
                    PatientId = pkg.PatientId,
                    TotalAppointments = pkg.TotalAppointments,
                    RemainingAppointments = pkg.RemainingAppointments,
                    Price = pkg.Price,
                    Status = pkg.Status,
                    PaymentMethod = pkg.PaymentMethod,
                    CreatedAt = pkg.CreatedAt,
                }
            },
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<AppointmentPackageDialog>(
            string.Empty,
            parameters,
            options
        );
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: AppointmentPackage updatedPackage })
        {
            var response = await PackageHandler.UpdatePackage(updatedPackage);

            if (response.Success)
            {
                Snackbar.Add("Pacote atualizado com sucesso!", Severity.Success);
                _packagesLoaded = false;
                await LoadPackagesAsync();
            }
            else
            {
                Snackbar.Add($"Erro ao atualizar pacote: {response.Message}", Severity.Warning);
            }
        }
    }

    private async Task DeletePackageAsync(AppointmentPackageListItemDto pkg)
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, BackdropClick = false, CloseButton = true };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar este pacote de {pkg.TotalAppointments} sessões? Esta ação afetará suas métricas financeiras permanentemente.",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var response = await PackageHandler.DeletePackage(pkg.Id);
            if (response.Success)
            {
                Snackbar.Add("Pacote removido com sucesso.", Severity.Success);
                _packagesLoaded = false;
                await LoadPackagesAsync();
            }
            else
            {
                Snackbar.Add($"Erro ao excluir pacote: {response.Message}", Severity.Error);
            }
        }
    }

    private async Task ExportRecords()
    {
        _isExporting = true;
        StateHasChanged();

        try
        {
            var bytes = await ExportHandler.ExportPatientRecords(Patient.Id, _exportFormat);

            var contentType = _exportFormat == "pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            var fileName = $"prontuario-{Patient.Name.Replace(" ", "-").ToLowerInvariant()}.{_exportFormat}";
            var base64 = Convert.ToBase64String(bytes);

            await JsRuntime.InvokeVoidAsync("downloadFile", fileName, contentType, base64);

            Snackbar.Add("Prontuário exportado com sucesso!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erro ao exportar prontuário: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private async Task RectifyRecord(AppointmentRecordListItemDto record)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<RectificationDialog>(
            "Retificar Prontuário",
            options
        );
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: string rectificationNote })
        {
            var updateRecord = new AppointmentRecord { Id = record.Id, Note = rectificationNote };

            var updateResult = await RecordHandler.UpdateAppointmentRecord(updateRecord);

            if (updateResult.Success)
            {
                Snackbar.Add("Retificação efetuada com sucesso!", Severity.Success);

                _records.Clear();
                _recordPage = 0;
                _recordsLoaded = false;
                await LoadRecordsAsync();
                StateHasChanged();
            }
            else
            {
                Snackbar.Add($"Erro ao gerar retificação: {updateResult.Message}", Severity.Error);
            }
        }
    }
}
