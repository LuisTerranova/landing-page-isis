using landing_page_isis.Components.Dialogs;
using landing_page_isis.Components.Helpers;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class ContractsView : ComponentBase
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IContractHandler ContractHandler { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private GenericTable<ContractListItemDto> _table = null!;

    private async Task<TableData<ContractListItemDto>> ServerReload(
        TableState state,
        CancellationToken ct
    )
    {
        try
        {
            var result = await ContractHandler.GetContracts(state.Page, state.PageSize, ct);

            return new TableData<ContractListItemDto>
            {
                TotalItems = result.TotalItems,
                Items = result.Items,
            };
        }
        catch (OperationCanceledException)
        {
            return new TableData<ContractListItemDto>();
        }
    }

    private async Task DeleteContract(ContractListItemDto contract)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            BackdropClick = false,
            CloseButton = true,
        };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar o contrato de {contract.PatientName}?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await ContractHandler.DeleteContract(contract.Id);

            if (result.Success)
            {
                Snackbar.Add("Contrato removido com sucesso.", Severity.Success);
                await _table.ReloadAsync();
            }
            else
            {
                Snackbar.Add("Erro ao excluir o contrato.", Severity.Error);
            }
        }
    }

    private async Task CopyLink(ContractListItemDto dto)
    {
        var fullContract = await ContractHandler.GetContract(dto.Id);
        if (fullContract?.AcceptanceToken == null)
        {
            Snackbar.Add("Contrato não possui link de aceitação.", Severity.Warning);
            return;
        }

        var link = $"{Navigation.BaseUri}contrato/{fullContract.AcceptanceToken}";

        try
        {
            await Js.InvokeVoidAsync("navigator.clipboard.writeText", link);
            Snackbar.Add("Link de aceitação copiado para a área de transferência!", Severity.Success);
        }
        catch
        {
            Snackbar.Add($"Não foi possível copiar automaticamente. Link: {link}", Severity.Warning);
        }
    }

    private async Task OpenEdit(ContractListItemDto dto)
    {
        var fullContract = await ContractHandler.GetContract(dto.Id);
        if (fullContract == null)
            return;

        var parameters = new DialogParameters<ContractDialog>
        {
            { x => x.Model, fullContract }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true,
        };

        var dialog = await DialogService.ShowAsync<ContractDialog>("Editar Contrato", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await _table.ReloadAsync();
        }
    }

    private async Task OpenViewDocument(ContractListItemDto dto)
    {
        var fullContract = await ContractHandler.GetContract(dto.Id);
        if (fullContract == null || string.IsNullOrEmpty(fullContract.ContractDocumentHtml))
        {
            Snackbar.Add("Documento ainda não foi gerado.", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters<DocumentViewDialog>
        {
            { x => x.HtmlContent, fullContract.ContractDocumentHtml }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true,
        };

        await DialogService.ShowAsync<DocumentViewDialog>("Visualizar Contrato", parameters, options);
    }

    private static Color GetStatusColor(ContractStatus status) => status switch
    {
        ContractStatus.Rascunho => Color.Default,
        ContractStatus.AguardandoAceitacao => Color.Warning,
        ContractStatus.Ativo => Color.Success,
        ContractStatus.Cancelado => Color.Error,
        _ => Color.Default,
    };
}
