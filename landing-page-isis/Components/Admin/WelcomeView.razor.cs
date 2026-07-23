using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace landing_page_isis.Components.Admin;

public partial class WelcomeView : ComponentBase
{
    #region Services

    [Inject]
    private IAppointmentHandler AppointmentHandler { get; set; } = null!;

    [Inject]
    private ILogger<WelcomeView> Logger { get; set; } = null!;

    #endregion

    #region Properties

    private List<AppointmentViewModel> _appointments = [];
    private bool _loading = true;
    private ViewMode _currentView = ViewMode.Day;
    private DateTime _selectedDate = DateTime.Today;

    private string _searchQuery = string.Empty;
    private bool IsSearching => !string.IsNullOrWhiteSpace(_searchQuery);

    private int _currentPage = 1;
    private int _pageSize = 9;
    private int _totalItems = 0;
    private int PendingRecordsCount { get; set; }

    #endregion

    #region Methods

    protected override async Task OnInitializedAsync()
    {
        _selectedDate = DateTime.UtcNow.ToPortoVelhoTime().Date;
        await LoadPendingCount();
        await LoadAppointments();
    }

    private async Task LoadPendingCount()
    {
        PendingRecordsCount = await AppointmentHandler.CountPendingRecordsAsync();
        StateHasChanged();
    }

    private async Task OnSearch(string query)
    {
        _searchQuery = query;
        _currentPage = 1;
        await LoadAppointments();
    }

    private async Task OnPageChanged(int page)
    {
        _currentPage = page;
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        _loading = true;
        StateHasChanged();
        try
        {
            if (IsSearching)
            {
                var queryResult = await AppointmentHandler.QueryAppointments(
                    _searchQuery,
                    _currentPage - 1,
                    _pageSize,
                    default
                );
                _totalItems = queryResult.TotalItems;
                _appointments = queryResult
                    .Items.Select(a => new AppointmentViewModel
                    {
                        Id = a.Id,
                        Date = a.AppointmentDate,
                        PatientName = a.PatientName ?? "N/A",
                        Status = a.AppointmentStatus,
                        Price = a.Price,
                        IsCouple = a.CoupleId.HasValue,
                    })
                    .OrderByDescending(a => a.Date)
                    .ToList();
            }
            else
            {
                DateTimeOffset start,
                    end;

                if (_currentView == ViewMode.Day)
                {
                    var date = _selectedDate.Date;
                    var startPvh = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                    var endPvh = startPvh.AddDays(1).AddTicks(-1);

                    start = startPvh.ToPortoVelhoDateTimeOffset();
                    end = endPvh.ToPortoVelhoDateTimeOffset();
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

                    start = startPvh.ToPortoVelhoDateTimeOffset();
                    end = endPvh.ToPortoVelhoDateTimeOffset();
                }

                var paginatedResult = await AppointmentHandler.GetAppointmentsByDateRange(
                    start,
                    end,
                    _currentPage - 1,
                    _pageSize,
                    default
                );

                _totalItems = paginatedResult.TotalItems;
                _appointments = paginatedResult
                    .Items.Select(a => new AppointmentViewModel
                    {
                        Id = a.Id,
                        Date = a.AppointmentDate,
                        PatientName = a.PatientName ?? "N/A",
                        Status = a.AppointmentStatus,
                        Price = a.Price,
                        IsCouple = a.CoupleId.HasValue,
                    })
                    .OrderBy(a => a.Date)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading appointments");
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
        _currentPage = 1;
        await LoadAppointments();
    }

    private async Task ChangeDate(int days)
    {
        _selectedDate = _selectedDate.AddDays(days);
        _currentPage = 1;
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
        public bool IsCouple { get; set; }
    }

    #endregion
}
