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
    private GenericTable<PatientListItemDto> _patientsTable = null!;
    private GenericTable<CoupleListItemDto> _couplesTable = null!;
    private string _searchQuery = string.Empty;
    private string _coupleSearchQuery = string.Empty;

    #endregion

    #region Methods - Individuals

    private async Task<TableData<PatientListItemDto>> ServerReload(
        TableState state,
        CancellationToken ct
    )
    {
        try
        {
            PaginatedResponse<PatientListItemDto> result;
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                result = await PatientHandler.GetPatients(state.Page, state.PageSize, ct);
            }
            else
            {
                result = await PatientHandler.QueryPatients(
                    _searchQuery,
                    state.Page,
                    state.PageSize,
                    ct
                );
            }

            return new TableData<PatientListItemDto>
            {
                TotalItems = result.TotalItems,
                Items = result.Items,
            };
        }
        catch (OperationCanceledException)
        {
            return new TableData<PatientListItemDto>();
        }
    }

    private async Task DeletePatient(PatientListItemDto patient)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            BackdropClick = false,
            CloseButton = true,
        };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar os dados de {patient.Name}?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await PatientHandler.DeletePatient(patient.Id);

            if (result.Success)
            {
                Snackbar.Add("Paciente removido com sucesso.", Severity.Success);
                await _patientsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Erro ao excluir o paciente.", Severity.Error);
            }
        }
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
                await _patientsTable.ReloadAsync();
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
                    PatientName = couple.Name,
                    PatientCpf = couple.PayerCpf,
                    PatientPhone = couple.Patient1.Phone,
                    PatientEmail = couple.Patient1.Email,
                    PatientState = couple.Patient1.StateOfResidency,
                    TermsAccepted = couple.PolicySigned,
                };

                var createResult = await ContractHandler.CreateContract(contractModel);
                if (!createResult.Success)
                {
                    Snackbar.Add($"Erro ao criar contrato: {createResult.Message}", Severity.Error);
                    return;
                }
            }

            var parameters = new DialogParameters<ContractDialog>
            {
                { x => x.Model, contractModel }
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
                await _patientsTable.ReloadAsync();
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
                    PatientName = fullPatient.Name,
                    PatientCpf = fullPatient.Cpf,
                    PatientEmail = fullPatient.Email,
                    PatientPhone = fullPatient.Phone,
                    PatientState = fullPatient.StateOfResidency,
                    PatientBirthDate = fullPatient.BirthDate,
                    TermsAccepted = fullPatient.PolicySigned,
                };

                var createResult = await ContractHandler.CreateContract(contractModel);
                if (!createResult.Success)
                {
                    Snackbar.Add($"Erro ao criar contrato: {createResult.Message}", Severity.Error);
                    return;
                }
            }

            var parameters = new DialogParameters<ContractDialog>
            {
                { x => x.Model, contractModel }
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
                await _patientsTable.ReloadAsync();
            }
        }
    }

    #endregion

    #region Methods - Couples

    private async Task<TableData<CoupleListItemDto>> CoupleServerReload(
        TableState state,
        CancellationToken ct
    )
    {
        try
        {
            PaginatedResponse<CoupleListItemDto> result;
            if (string.IsNullOrWhiteSpace(_coupleSearchQuery))
            {
                result = await CoupleHandler.GetCouples(state.Page, state.PageSize, ct);
            }
            else
            {
                result = await CoupleHandler.QueryCouples(
                    _coupleSearchQuery,
                    state.Page,
                    state.PageSize,
                    ct
                );
            }

            return new TableData<CoupleListItemDto>
            {
                TotalItems = result.TotalItems,
                Items = result.Items,
            };
        }
        catch (OperationCanceledException)
        {
            return new TableData<CoupleListItemDto>();
        }
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
