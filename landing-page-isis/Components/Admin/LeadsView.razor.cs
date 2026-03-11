using landing_page_isis.Components.Dialogs;
using landing_page_isis.Components.Helpers;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class LeadsView : ComponentBase
{
    #region Services

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ILeadHandler LeadHandler { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    #endregion

    #region Properties

    private GenericTable<Lead> _leadsTable = null!;

    #endregion

    #region Methods

    private async Task<TableData<Lead>> ServerReload(TableState state, CancellationToken ct)
    {
        try
        {
            var result = await LeadHandler.GetLeads(state.Page, state.PageSize, ct);

            return new TableData<Lead>
            {
                TotalItems = result.TotalItems,
                Items = result.Items.Where(l => l != null).Cast<Lead>(),
            };
        }
        catch (OperationCanceledException)
        {
            return new TableData<Lead>();
        }
    }

    private async Task ContactViaWhatsApp(Lead lead)
    {
        var url = LeadHandler.GetWhatsAppUrl(lead);
        if (!string.IsNullOrEmpty(url))
        {
            await JsRuntime.InvokeVoidAsync("open", url, "_blank");
        }
        else
        {
            Snackbar.Add("Este lead não possui um número de telefone válido.", Severity.Warning);
        }
    }

    private async Task ViewIntent(Lead lead)
    {
        var parameters = new DialogParameters<LeadIntentDialog> { { x => x.Lead, lead } };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
        };

        await DialogService.ShowAsync<LeadIntentDialog>("Visualizar Intenção", parameters, options);
    }

    private async Task DeleteLead(Lead lead)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar os dados de {lead.Name}?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await LeadHandler.DeleteLead(lead.Id);

            if (result.Success)
            {
                Snackbar.Add("Lead removido com sucesso.", Severity.Success);
                await _leadsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add("Erro ao excluir o lead.", Severity.Error);
            }
        }
    }

    private async Task ApproveLead(Lead lead)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Aprovação",
            $"Aprovar lead de {lead.Name}? Esta ação desencadeará a criação de um paciente.",
            yesText: "Aprovar",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await LeadHandler.ApproveLead(lead.Id);

            if (result.Success)
            {
                Snackbar.Add("Lead aprovado com sucesso.", Severity.Success);
                await _leadsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add("Erro ao aprovar o lead.", Severity.Error);
            }
        }
    }

    #endregion
}
