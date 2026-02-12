using landing_page_isis.Components.Misc;
using landing_page_isis.core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class PacientsView : ComponentBase
{
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private PacientHandler PacientHandler { get; set; } = null!;

    private GenericTable<Pacient> _pacientsTable = null!;
    private bool _loading = true;
    
private async Task<TableData<Pacient>> ServerReload(TableState state, CancellationToken ct)
{
    try
    {
        var result = await PacientHandler.GetPacients(state.Page, state.PageSize, ct);

        return new TableData<Pacient>
        { 
            TotalItems = result.TotalItems, 
            Items = result.Items 
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
        
        var confirm = await DialogService.ShowMessageBox(
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
    
}
}

