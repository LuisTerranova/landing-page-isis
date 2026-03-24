using landing_page_isis.Components.Dialogs;
using landing_page_isis.Components.Helpers;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class AppointmentsView : ComponentBase
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IAppointmentHandler AppointmentHandler { get; set; } = null!;

    private GenericTable<Appointment> _appointmentsTable = null!;
    private string _searchQuery = string.Empty;

    private async Task<TableData<Appointment>> ServerReload(TableState state, CancellationToken ct)
    {
        try
        {
            PaginatedResponse<Appointment?> result;
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                result = await AppointmentHandler.GetAllAppointments(
                    state.Page,
                    state.PageSize,
                    ct
                );
            }
            else
            {
                result = await AppointmentHandler.QueryAppointments(
                    _searchQuery,
                    state.Page,
                    state.PageSize,
                    ct
                );
            }
            return new TableData<Appointment>
            {
                TotalItems = result.TotalItems,
                Items = result.Items.Where(a => a != null).Cast<Appointment>(),
            };
        }
        catch (OperationCanceledException)
        {
            return new TableData<Appointment>();
        }
    }

    private async Task OpenCreate()
    {
        var parameters = new DialogParameters<AppointmentDialog>
        {
            { x => x.Titulo, "Novo Agendamento" },
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var dialog = await DialogService.ShowAsync<AppointmentDialog>(
            "Novo Agendamento",
            parameters,
            options
        );
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: Appointment model })
        {
            var success = await AppointmentHandler.CreateAppointment(model);
            if (success.Success)
            {
                Snackbar.Add("Agendamento criado!", Severity.Success);
                await _appointmentsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add(success.Message ?? "Erro ao criar agendamento.", Severity.Error);
            }
        }
    }

    private async Task EditAppointment(Appointment appointment)
    {
        var parameters = new DialogParameters<AppointmentDialog>
        {
            { x => x.Titulo, "Editar Agendamento" },
            {
                x => x.Model,
                new Appointment
                {
                    Id = appointment.Id,
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentStatus = appointment.AppointmentStatus,
                    Price = appointment.Price,
                    PacientId = appointment.PacientId,
                }
            },
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };
        var dialog = await DialogService.ShowAsync<AppointmentDialog>(
            "Editar Agendamento",
            parameters,
            options
        );
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: Appointment editedModel })
        {
            var success = await AppointmentHandler.UpdateAppointment(editedModel, editedModel.Id);
            if (success.Success)
            {
                Snackbar.Add("Agendamento atualizado!", Severity.Success);
                await _appointmentsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add($"Erro ao atualizar: {success.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteAppointment(Appointment appointment)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Confirmar Exclusão",
            $"Tem certeza que deseja apagar o agendamento de {appointment.AppointmentDate.ToLocalTime():dd/MM/yyyy HH:mm}?",
            yesText: "Excluir",
            cancelText: "Cancelar",
            options: options
        );

        if (confirm == true)
        {
            var result = await AppointmentHandler.DeleteAppointment(appointment.Id);

            if (result.Success)
            {
                Snackbar.Add("Agendamento removido com sucesso.", Severity.Success);
                await _appointmentsTable.ReloadAsync();
            }
            else
            {
                Snackbar.Add($"Erro ao excluir: {result.Message}", Severity.Error);
            }
        }
    }
}
