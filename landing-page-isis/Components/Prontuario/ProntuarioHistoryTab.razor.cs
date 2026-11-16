using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using landing_page_isis.Components.Dialogs.Patient;
using landing_page_isis.Components.Dialogs.Prontuario;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using PatientModel = landing_page_isis.core.Models.Patient;

namespace landing_page_isis.Components.Prontuario;

public partial class ProntuarioHistoryTab : ComponentBase
{
    [Parameter]
    public PatientModel? Patient { get; set; }

    [Parameter]
    public Couple? Couple { get; set; }

    [Inject]
    private IProntuarioHandler ProntuarioHandler { get; set; } = null!;

    [Inject]
    private IAdditionalNoteHandler AdditionalNoteHandler { get; set; } = null!;

    [Inject]
    private IAppointmentRecordHandler RecordHandler { get; set; } = null!;

    [Inject]
    private IAppointmentRecordExportHandler ExportHandler { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    private List<ProntuarioTimelineItemDto> _items = [];
    private int _page;
    private int _pageSize = 5;
    private int _totalItems;
    private bool _loading;
    private bool _loadingMore;
    private DateTime? _filterMonthYear;
    private bool _isExporting;
    private Guid? _loadedOwnerId;
    private int _loadVersion;

    private bool IsCouple => Couple is not null;
    private Guid OwnerId => Couple?.Id ?? Patient!.Id;
    private string OwnerName => IsCouple ? Couple!.Name : Patient!.Name;
    private string Title => IsCouple ? "Histórico do Casal" : "Histórico do Paciente";

    private static DialogOptions FormDialogOptions =>
        new()
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            BackdropClick = false,
            CloseButton = true,
        };

    protected override async Task OnParametersSetAsync()
    {
        var ownerId = OwnerId;
        if (_loadedOwnerId == ownerId)
            return;

        _loadedOwnerId = ownerId;
        await ReloadAsync();
    }

    private async Task OnFilterChangedAsync(DateTime? value)
    {
        _filterMonthYear = value;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var version = ++_loadVersion;
        _loading = true;
        _loadingMore = false;
        _page = 0;
        _items.Clear();
        _totalItems = 0;
        StateHasChanged();

        try
        {
            var result = await LoadPageAsync(0);
            if (version != _loadVersion)
                return;

            _items = result.Items.ToList();
            _totalItems = result.TotalItems;
        }
        catch (Exception ex)
        {
            if (version == _loadVersion)
                Snackbar.Add($"Erro ao carregar prontuário: {ex.Message}", Severity.Error);
        }
        finally
        {
            if (version == _loadVersion)
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_loading || _loadingMore)
            return;

        var version = _loadVersion;
        var nextPage = _page + 1;
        _loadingMore = true;
        StateHasChanged();

        try
        {
            var result = await LoadPageAsync(nextPage);
            if (version != _loadVersion)
                return;

            _page = nextPage;
            _items.AddRange(result.Items);
            _totalItems = result.TotalItems;
        }
        catch (Exception ex)
        {
            if (version == _loadVersion)
                Snackbar.Add($"Erro ao carregar prontuário: {ex.Message}", Severity.Error);
        }
        finally
        {
            if (version == _loadVersion)
            {
                _loadingMore = false;
                StateHasChanged();
            }
        }
    }

    private Task<PaginatedResponse<ProntuarioTimelineItemDto>> LoadPageAsync(int page)
    {
        return IsCouple
            ? ProntuarioHandler.GetTimelineByCoupleId(
                page,
                _pageSize,
                Couple!.Id,
                _filterMonthYear,
                CancellationToken.None
            )
            : ProntuarioHandler.GetTimelineByPatientId(
                page,
                _pageSize,
                Patient!.Id,
                _filterMonthYear,
                CancellationToken.None
            );
    }

    private async Task OpenCreateNoteAsync()
    {
        var dialog = await DialogService.ShowAsync<AdditionalNoteDialog>(
            "Nova Nota Avulsa",
            FormDialogOptions
        );
        var result = await dialog.Result;

        if (result is not { Canceled: false, Data: AdditionalNoteFormResult form })
            return;

        var response = IsCouple
            ? await AdditionalNoteHandler.Create(
                null,
                Couple!.Id,
                form.Content,
                form.NoteDate,
                CancellationToken.None
            )
            : await AdditionalNoteHandler.Create(
                Patient!.Id,
                null,
                form.Content,
                form.NoteDate,
                CancellationToken.None
            );

        if (response.Success)
        {
            Snackbar.Add("Nota avulsa criada com sucesso!", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add($"Erro ao criar nota avulsa: {response.Message}", Severity.Error);
        }
    }

    private async Task EditNoteAsync(ProntuarioTimelineItemDto item)
    {
        var note = await AdditionalNoteHandler.GetById(item.Id, CancellationToken.None);
        if (note is null)
        {
            Snackbar.Add("Nota avulsa não encontrada.", Severity.Error);
            await ReloadAsync();
            return;
        }

        var parameters = new DialogParameters<AdditionalNoteDialog> { { x => x.Note, note } };
        var dialog = await DialogService.ShowAsync<AdditionalNoteDialog>(
            "Editar Nota Avulsa",
            parameters,
            FormDialogOptions
        );
        var result = await dialog.Result;

        if (result is not { Canceled: false, Data: AdditionalNoteFormResult form })
            return;

        var response = await AdditionalNoteHandler.Update(
            item.Id,
            form.Content,
            form.NoteDate,
            CancellationToken.None
        );

        if (response.Success)
        {
            Snackbar.Add("Nota avulsa atualizada com sucesso!", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add($"Erro ao atualizar nota avulsa: {response.Message}", Severity.Error);
        }
    }

    private async Task DeleteNoteAsync(ProntuarioTimelineItemDto item)
    {
        var confirm = await DialogService.ShowMessageBoxAsync(
            "Excluir Nota Avulsa",
            "Tem certeza que deseja excluir esta nota avulsa?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                BackdropClick = false,
                CloseButton = true,
            }
        );

        if (confirm != true)
            return;

        var response = await AdditionalNoteHandler.Delete(item.Id, CancellationToken.None);
        if (response.Success)
        {
            Snackbar.Add("Nota avulsa excluída com sucesso.", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add($"Erro ao excluir nota avulsa: {response.Message}", Severity.Error);
        }
    }

    private async Task RectifyAsync(ProntuarioTimelineItemDto item)
    {
        var dialog = await DialogService.ShowAsync<RectificationDialog>(
            "Retificar Prontuário",
            FormDialogOptions
        );
        var result = await dialog.Result;

        if (result is not { Canceled: false, Data: string rectificationNote })
            return;

        var response = await RecordHandler.UpdateAppointmentRecord(
            new AppointmentRecord { Id = item.Id, Note = rectificationNote }
        );

        if (response.Success)
        {
            Snackbar.Add("Retificação efetuada com sucesso!", Severity.Success);
            await ReloadAsync();
        }
        else
        {
            Snackbar.Add($"Erro ao gerar retificação: {response.Message}", Severity.Error);
        }
    }

    private async Task OpenExportDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<ExportRecordsDialog>(
            "Exportar Prontuário",
            FormDialogOptions
        );
        var result = await dialog.Result;

        if (result is not { Canceled: false, Data: ExportRecordsOptions options })
            return;

        _isExporting = true;
        StateHasChanged();

        try
        {
            var bytes = IsCouple
                ? await ExportHandler.ExportCoupleRecords(
                    Couple!.Id,
                    options.Format,
                    options.IncludeNotes,
                    CancellationToken.None
                )
                : await ExportHandler.ExportPatientRecords(
                    Patient!.Id,
                    options.Format,
                    options.IncludeNotes,
                    CancellationToken.None
                );

            var contentType =
                options.Format == "pdf"
                    ? "application/pdf"
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            var fileName = $"prontuario-{NormalizeFileName(OwnerName)}.{options.Format}";
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

    private static string NormalizeFileName(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC);
        var slug = Regex
            .Replace(withoutDiacritics, "[^a-zA-Z0-9]+", "-")
            .Trim('-')
            .ToLowerInvariant();

        return string.IsNullOrWhiteSpace(slug) ? "registro" : slug;
    }
}
