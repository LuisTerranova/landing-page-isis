using landing_page_isis.Components.Dialogs;
using landing_page_isis.Components.Helpers;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class PacientsView : ComponentBase
{
    #region Services

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IPacientHandler PacientHandler { get; set; } = null!;

    #endregion

    #region Properties

    private GenericTable<Pacient> _pacientsTable = null!;

    #endregion

    #region Methods

    private async Task<TableData<Pacient>> ServerReload(TableState state, CancellationToken ct)
    {
        try
        {
            var result = await PacientHandler.GetPacients(state.Page, state.PageSize, ct);

            return new TableData<Pacient>
            {
                TotalItems = result.TotalItems,
                Items = result.Items.Where(p => p != null).Cast<Pacient>(),
            };
        }
        catch (OperationCanceledException)
        {
            return new TableData<Pacient>();
        }
    }

    private async Task DeletePacient(Pacient pacient)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar os dados de {pacient.Name}?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await PacientHandler.DeletePacient(pacient.Id);

            if (result.Success)
            {
                Snackbar.Add("Paciente removido com sucesso.", Severity.Success);
                await _pacientsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add("Erro ao excluir o paciente.", Severity.Error);
            }
        }
    }

    private async Task EditPacient(Pacient pacient)
    {
        var parameters = new DialogParameters<PacientDialog>
        {
            { x => x.Titulo, "Editar Paciente" },
            {
                x => x.Model,
                new Pacient
                {
                    Id = pacient.Id,
                    Name = pacient.Name,
                    Cpf = pacient.Cpf,
                    BirthDate = pacient.BirthDate,
                    Email = pacient.Email,
                    Phone = pacient.Phone,
                    StateOfResidency = pacient.StateOfResidency,
                }
            },
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var dialog = await DialogService.ShowAsync<PacientDialog>("Edição", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false } && result.Data is Pacient pacientEditado)
        {
            var sucesso = await PacientHandler.UpdatePacient(pacientEditado);
            if (sucesso.Success)
            {
                Snackbar.Add("Paciente atualizado!", Severity.Success);
                await _pacientsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add($"Erro ao atualizar: {sucesso.Message}", Severity.Error);
            }
        }
    }

    private async Task OpenCreate()
    {
        var parameters = new DialogParameters<PacientDialog> { { x => x.Titulo, "Novo Paciente" } };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var dialog = await DialogService.ShowAsync<PacientDialog>("Cadastro", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false } && result.Data is Pacient novoPaciente)
        {
            var sucesso = await PacientHandler.CreatePacient(novoPaciente);
            if (sucesso.Success)
            {
                Snackbar.Add("Paciente salvo!", Severity.Success);
                await _pacientsTable.ReloadAsync();
            }
        }
    }

    #endregion
}
