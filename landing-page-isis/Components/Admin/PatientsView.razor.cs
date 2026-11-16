using landing_page_isis.Components.Dialogs;
using landing_page_isis.Components.Dialogs.Couple;
using landing_page_isis.Components.Dialogs.Patient;
using landing_page_isis.Components.Helpers;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class PatientsView : ComponentBase
{
    #region Services

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IPatientHandler PatientHandler { get; set; } = null!;

    [Inject]
    private ICoupleHandler CoupleHandler { get; set; } = null!;

    [Inject]
    private IContractHandler ContractHandler { get; set; } = null!;

    #endregion

    #region Properties

    private bool ShowCouples { get; set; }
    private GenericTable<PatientListItemDto>? _activePatientsTable;
    private GenericTable<PatientListItemDto>? _inactivePatientsTable;
    private GenericTable<CoupleListItemDto> _couplesTable = null!;
    private int _patientTabIndex;
    private string _activeSearchQuery = string.Empty;
    private string _coupleSearchQuery = string.Empty;

    // Blazor Server circuits share one scoped DbContext. Serialize table queries so
    // switching tabs cannot start overlapping EF Core operations on the same instance.
    private readonly SemaphoreSlim _queryLock = new(1, 1);

    #endregion

    #region Methods - Individuals

    private Task<TableData<PatientListItemDto>> ActiveServerReload(
        TableState state,
        CancellationToken ct
    )
    {
        return QueryWithLockAsync(
            token =>
                string.IsNullOrWhiteSpace(_activeSearchQuery)
                    ? PatientHandler.GetPatients(
                        state.Page,
                        state.PageSize,
                        token,
                        PatientStatusFilter.Active
                    )
                    : PatientHandler.QueryPatients(
                        _activeSearchQuery,
                        state.Page,
                        state.PageSize,
                        token,
                        includeDeactivated: false
                    ),
            ct
        );
    }

    private Task<TableData<PatientListItemDto>> InactiveServerReload(
        TableState state,
        CancellationToken ct
    )
    {
        return QueryWithLockAsync(
            token =>
                PatientHandler.GetPatients(
                    state.Page,
                    state.PageSize,
                    token,
                    PatientStatusFilter.Inactive
                ),
            ct
        );
    }

    private async Task<TableData<T>> QueryWithLockAsync<T>(
        Func<CancellationToken, Task<PaginatedResponse<T>>> query,
        CancellationToken ct
    )
    {
        try
        {
            await _queryLock.WaitAsync(ct);
            try
            {
                var result = await query(ct);
                return new TableData<T> { TotalItems = result.TotalItems, Items = result.Items };
            }
            finally
            {
                _queryLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return new TableData<T>();
        }
    }

    private async Task OnPatientTabChanged(int index)
    {
        _patientTabIndex = index;

        if (index == 0 && _activePatientsTable is not null)
        {
            await _activePatientsTable.ReloadAsync();
        }
        else if (index == 1 && _inactivePatientsTable is not null)
        {
            await _inactivePatientsTable.ReloadAsync();
        }
    }

    private async Task DeactivatePatient(PatientListItemDto patient)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            BackdropClick = false,
            CloseButton = true,
        };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Desativar Paciente",
            $"Desativar {patient.Name}? O histórico, contrato e pacotes serão preservados.",
            yesText: "Desativar",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm != true)
            return;

        var result = await PatientHandler.SetPatientActive(
            patient.Id,
            false,
            CancellationToken.None
        );

        if (result.Success)
        {
            Snackbar.Add("Paciente desativado com sucesso.", Severity.Success);
            await ReloadPatientTables();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Erro ao desativar o paciente.", Severity.Error);
        }
    }

    private async Task ReactivatePatient(PatientListItemDto patient)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            BackdropClick = false,
            CloseButton = true,
        };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Reativar Paciente",
            $"Reativar {patient.Name}? O paciente voltará a aparecer nas listas ativas.",
            yesText: "Reativar",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm != true)
            return;

        var result = await PatientHandler.SetPatientActive(
            patient.Id,
            true,
            CancellationToken.None
        );

        if (result.Success)
        {
            Snackbar.Add("Paciente reativado com sucesso.", Severity.Success);
            await ReloadPatientTables();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Erro ao reativar o paciente.", Severity.Error);
        }
    }

    private async Task ReloadPatientTables()
    {
        if (_activePatientsTable is not null)
            await _activePatientsTable.ReloadAsync();

        if (_inactivePatientsTable is not null)
            await _inactivePatientsTable.ReloadAsync();
    }

    private async Task OpenCreate()
    {
        var parameters = new DialogParameters<PatientDialog> { { x => x.Titulo, "Novo Paciente" } };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true,
        };

        var dialog = await DialogService.ShowAsync<PatientDialog>("Cadastro", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false } && result.Data is Patient novoPaciente)
        {
            var sucesso = await PatientHandler.CreatePatient(novoPaciente);
            if (sucesso.Success)
            {
                Snackbar.Add("Paciente salvo!", Severity.Success);
                if (_activePatientsTable is not null)
                    await _activePatientsTable.ReloadAsync();
            }
        }
    }

    private async Task OpenDetails(PatientListItemDto dto)
    {
        var fullPatient = await PatientHandler.GetPatient(dto.Id);
        if (fullPatient == null)
            return;

        var parameters = new DialogParameters<DetailsDialog> { { x => x.Patient, fullPatient } };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            BackdropClick = false,
        };

        var dialog = await DialogService.ShowAsync<DetailsDialog>(
            string.Empty,
            parameters,
            options
        );
        await dialog.Result;
    }

    private async Task OpenCoupleDetails(CoupleListItemDto dto)
    {
        var fullCouple = await CoupleHandler.GetCouple(dto.Id);
        if (fullCouple == null)
            return;

        var parameters = new DialogParameters<DetailsDialog> { { x => x.Couple, fullCouple } };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            BackdropClick = false,
        };

        var dialog = await DialogService.ShowAsync<DetailsDialog>(
            string.Empty,
            parameters,
            options
        );
        await dialog.Result;
    }

    private async Task HandleContract(PatientListItemDto dto)
    {
        var fullPatient = await PatientHandler.GetPatient(dto.Id);
        if (fullPatient == null)
            return;

        var couple = await CoupleHandler.GetCoupleByPatientId(dto.Id);

        if (couple != null)
        {
            var existingContract = await ContractHandler.GetContractByCoupleId(couple.Id);

            Contract contractModel;
            if (existingContract != null)
            {
                contractModel = existingContract;
            }
            else
            {
                contractModel = new Contract
                {
                    CoupleId = couple.Id,
                    PrimaryPatient = new ContractParticipantInfo
                    {
                        Name = couple.Name,
                        Cpf = couple.PayerCpf,
                        Phone = couple.Patient1.Phone,
                        Email = couple.Patient1.Email,
                        State = couple.Patient1.StateOfResidency,
                    },
                    TermsAccepted = true,
                };

                var createResult = await ContractHandler.CreateInsiderContract(contractModel);
                if (!createResult.Success)
                {
                    Snackbar.Add($"Erro ao criar contrato: {createResult.Message}", Severity.Error);
                    return;
                }
            }

            var parameters = new DialogParameters<ContractDialog>
            {
                { x => x.Model, contractModel },
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false,
                CloseButton = true,
            };

            var dialog = await DialogService.ShowAsync<ContractDialog>(
                "Editar Contrato",
                parameters,
                options
            );
            var result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("Contrato salvo com sucesso.", Severity.Success);
                await ReloadPatientTables();
            }
        }
        else
        {
            var existingContract = await ContractHandler.GetContractByPatientId(dto.Id);

            Contract contractModel;
            if (existingContract != null)
            {
                contractModel = existingContract;
            }
            else
            {
                contractModel = new Contract
                {
                    PatientId = dto.Id,
                    PrimaryPatient = new ContractParticipantInfo
                    {
                        Name = fullPatient.Name,
                        Cpf = fullPatient.Cpf,
                        Email = fullPatient.Email,
                        Phone = fullPatient.Phone,
                        State = fullPatient.StateOfResidency,
                        BirthDate = fullPatient.BirthDate,
                    },
                    TermsAccepted = true,
                };

                var createResult = await ContractHandler.CreateInsiderContract(contractModel);
                if (!createResult.Success)
                {
                    Snackbar.Add($"Erro ao criar contrato: {createResult.Message}", Severity.Error);
                    return;
                }
            }

            var parameters = new DialogParameters<ContractDialog>
            {
                { x => x.Model, contractModel },
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false,
                CloseButton = true,
            };

            var dialog = await DialogService.ShowAsync<ContractDialog>(
                "Editar Contrato",
                parameters,
                options
            );
            var result = await dialog.Result;

            if (result is { Canceled: false })
            {
                Snackbar.Add("Contrato salvo com sucesso.", Severity.Success);
                await ReloadPatientTables();
            }
        }
    }

    #endregion

    #region Methods - Couples

    private Task<TableData<CoupleListItemDto>> CoupleServerReload(
        TableState state,
        CancellationToken ct
    )
    {
        return QueryWithLockAsync(
            token =>
                string.IsNullOrWhiteSpace(_coupleSearchQuery)
                    ? CoupleHandler.GetCouples(state.Page, state.PageSize, token)
                    : CoupleHandler.QueryCouples(
                        _coupleSearchQuery,
                        state.Page,
                        state.PageSize,
                        token
                    ),
            ct
        );
    }

    private async Task OpenCreateCouple()
    {
        var dialog = await DialogService.ShowAsync<CoupleDialog>("Novo Casal");
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: Couple newCouple })
        {
            var response = await CoupleHandler.CreateCouple(newCouple);
            if (response.Success)
            {
                Snackbar.Add("Casal criado com sucesso!", Severity.Success);
                await _couplesTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Erro ao criar casal.", Severity.Error);
            }
        }
    }

    private async Task DeleteCouple(CoupleListItemDto couple)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            BackdropClick = false,
            CloseButton = true,
        };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar o casal {couple.Name}?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await CoupleHandler.DeleteCouple(couple.Id);

            if (result.Success)
            {
                Snackbar.Add("Casal removido com sucesso.", Severity.Success);
                await _couplesTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Erro ao excluir o casal.", Severity.Error);
            }
        }
    }

    #endregion
}
