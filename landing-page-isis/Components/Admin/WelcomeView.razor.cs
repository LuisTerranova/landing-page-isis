using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using Microsoft.AspNetCore.Components;

namespace landing_page_isis.Components.Admin;

public partial class WelcomeView : ComponentBase
{
    #region Services

    [Inject]
    private IAppointmentHandler AppointmentHandler { get; set; } = null!;

    #endregion

    #region Properties

    private List<AppointmentViewModel> _appointments = [];
    private bool _loading = true;
    private ViewMode _currentView = ViewMode.Day;
    private DateTime _selectedDate = DateTime.Today;

    #endregion

    #region Methods

    protected override async Task OnInitializedAsync()
    {
        var pvhTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Porto_Velho");
        _selectedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pvhTimeZone).Date;
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        _loading = true;
        StateHasChanged();
        try
        {
            var pvhTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Porto_Velho");
            DateTimeOffset start,
                end;

            if (_currentView == ViewMode.Day)
            {
                var date = _selectedDate.Date;
                // Calculate start and end of day in Porto Velho
                var startPvh = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                var endPvh = startPvh.AddDays(1).AddTicks(-1);

                start = new DateTimeOffset(
                    startPvh,
                    pvhTimeZone.GetUtcOffset(startPvh)
                ).ToUniversalTime();
                end = new DateTimeOffset(
                    endPvh,
                    pvhTimeZone.GetUtcOffset(endPvh)
                ).ToUniversalTime();
            }
            else
            {
                int diff = (7 + (_selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                var mondayDate = _selectedDate.AddDays(-1 * diff).Date;

                var startPvh = new DateTime(
                    mondayDate.Year,
                    mondayDate.Month,
                    mondayDate.Day,
                    0,
                    0,
                    0
                );
                var endPvh = startPvh.AddDays(7).AddTicks(-1);

                start = new DateTimeOffset(
                    startPvh,
                    pvhTimeZone.GetUtcOffset(startPvh)
                ).ToUniversalTime();
                end = new DateTimeOffset(
                    endPvh,
                    pvhTimeZone.GetUtcOffset(endPvh)
                ).ToUniversalTime();
            }

            var results = await AppointmentHandler.GetAppointmentsByDateRange(start, end, default);

            _appointments = results
                .Select(a => new AppointmentViewModel
                {
                    Id = a.Id,
                    Date = a.AppointmentDate,
                    PatientName = a.Pacient?.Name ?? "N/A",
                    Status = a.AppointmentStatus,
                    Price = a.Price,
                })
                .OrderBy(a => a.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading appointments: {ex.Message}");
            _appointments = [];
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task ToggleView(ViewMode mode)
    {
        if (_currentView == mode)
            return;
        _currentView = mode;
        await LoadAppointments();
    }

    private async Task ChangeDate(int days)
    {
        _selectedDate = _selectedDate.AddDays(days);
        await LoadAppointments();
    }

    private string GetViewTitle()
    {
        if (_currentView == ViewMode.Day)
            return _selectedDate.ToString("dd/MM/yyyy");

        var diff = (7 + (_selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = _selectedDate.AddDays(-1 * diff).Date;
        var sunday = monday.AddDays(6);
        return $"{monday:dd/MM} - {sunday:dd/MM}";
    }

    #endregion


    #region models

    private enum ViewMode
    {
        Day,
        Week,
    }

    public class AppointmentViewModel
    {
        public Guid Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public string PatientName { get; set; } = "";
        public AppointmentStatusEnum Status { get; set; }
        public decimal Price { get; set; }
    }

    #endregion
}
