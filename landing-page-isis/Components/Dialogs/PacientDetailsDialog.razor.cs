using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace landing_page_isis.Components.Dialogs;

public partial class PacientDetailsDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Pacient Pacient { get; set; } = null!;

    [Inject]
    private IAppointmentRecordHandler RecordHandler { get; set; } = null!;

    [Inject]
    private IAppointmentPackageHandler PackageHandler { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IPacientHandler PacientHandler { get; set; } = null!;

    private bool _showCpf;
    private bool _isEditing;
    private bool _isFormValid;
    private MudForm _perfilForm = null!;
    private Pacient _model = new();
    private DateTime? _tempBirthDate;

    // Records
    private DateTime? _recordsFilterDate;
    private bool _isLoadingRecords;
    private bool _recordsLoaded;
    private List<AppointmentRecord> _records = new();
    private int _recordPage;
    private int _totalRecords;
    private const int RecordsPageSize = 5;

    // Packages
    private bool _isLoadingPackages;
    private bool _packagesLoaded;
    private List<AppointmentPackage> _packages = new();

    protected override void OnParametersSet()
    {
        ResetModel();
    }

    private void ResetModel()
    {
        _model = new Pacient
        {
            Id = Pacient.Id,
            Name = Pacient.Name,
            Email = Pacient.Email,
            Phone = Pacient.Phone,
            Cpf = Pacient.Cpf,
            BirthDate = Pacient.BirthDate,
            StateOfResidency = Pacient.StateOfResidency,
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

    private async Task SavePacient()
    {
        if (_perfilForm != null)
        {
            await _perfilForm.ValidateAsync();
            if (!_isFormValid)
                return;
        }

        _model.BirthDate = _tempBirthDate.HasValue
            ? DateOnly.FromDateTime(_tempBirthDate.Value)
            : null;

        var result = await PacientHandler.UpdatePacient(_model);
        if (result.Success)
        {
            Snackbar.Add("Dados do paciente atualizados com sucesso!", Severity.Success);

            // Reflects changes on the parent object
            Pacient.Name = _model.Name;
            Pacient.Email = _model.Email;
            Pacient.Phone = _model.Phone;
            Pacient.Cpf = _model.Cpf;
            Pacient.BirthDate = _model.BirthDate;
            Pacient.StateOfResidency = _model.StateOfResidency;

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

        var result = await RecordHandler.GetRecordsByPacientId(
            _recordPage,
            RecordsPageSize,
            Pacient.Id,
            _recordsFilterDate,
            CancellationToken.None
        );

        _records.AddRange(result.Items.Where(r => r != null).Cast<AppointmentRecord>());
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

        var result = await PackageHandler.GetPackagesByPacientId(
            0,
            50,
            Pacient.Id,
            CancellationToken.None
        );
        _packages = result.Items.Where(p => p != null).Cast<AppointmentPackage>().ToList();

        _packagesLoaded = true;
        _isLoadingPackages = false;
    }

    private async Task OpenCreatePackage()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var dialog = await DialogService.ShowAsync<AppointmentPackageDialog>(string.Empty, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: AppointmentPackage newPackage })
        {
            newPackage.PacientId = Pacient.Id;
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

    private async Task OpenEditPackage(AppointmentPackage pkg)
    {
        var parameters = new DialogParameters<AppointmentPackageDialog>
        {
            {
                x => x.Model,
                new AppointmentPackage
                {
                    Id = pkg.Id,
                    PacientId = pkg.PacientId,
                    TotalAppointments = pkg.TotalAppointments,
                    RemainingAppointments = pkg.RemainingAppointments,
                    Price = pkg.Price,
                    Status = pkg.Status,
                    PaymentMethod = pkg.PaymentMethod,
                    CreatedAt = pkg.CreatedAt,
                    UpdatedAt = pkg.UpdatedAt,
                }
            },
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
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

    private async Task DeletePackageAsync(AppointmentPackage pkg)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };

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

    private async Task RectifyRecord(AppointmentRecord record)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
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

                var refreshed = await RecordHandler.GetAppointmentRecordById(record.Id);
                if (refreshed != null)
                {
                    record.Note = refreshed.Note;
                    record.UpdatedAt = refreshed.UpdatedAt;
                    StateHasChanged();
                }
            }
            else
            {
                Snackbar.Add($"Erro ao gerar retificação: {updateResult.Message}", Severity.Error);
            }
        }
    }
}
