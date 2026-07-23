using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class FinancesView : ComponentBase
{
    #region Services

    [Inject]
    private IAppointmentHandler AppointmentHandler { get; set; } = null!;

    [Inject]
    private IAppointmentPackageHandler PackageHandler { get; set; } = null!;

    [Inject]
    private ILogger<FinancesView> Logger { get; set; } = null!;

    #endregion

    #region Properties

    private bool _loading = true;

    // Filters
    private DateTime? _startDate;
    private DateTime? _endDate;

    private string _rangeLabel =>
        _startDate is not null && _endDate is not null
            ? $"{_startDate:dd MMM} → {_endDate:dd MMM yyyy}"
            : "";

    // KPIs
    private decimal _realizedRevenue;
    private decimal _pendingRevenue;
    private decimal _totalRevenue;
    private decimal _packagesRevenue;

    // Charts Data
    private List<ChartSeries<double>> _monthlyRevenueSeries = [];
    private string[] _monthlyRevenueLabels = [];

    #endregion

    #region Methods

    protected override async Task OnInitializedAsync()
    {
        var now = DateTime.UtcNow.ToPortoVelhoTime();
        _startDate = new DateTime(now.Year, now.Month, 1);
        _endDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        await LoadFinances();
    }

    private void ShiftDateRange(int months)
    {
        if (_startDate is null || _endDate is null)
            return;
        var daysDiff = (_endDate.Value - _startDate.Value).Days;
        _startDate = _startDate.Value.AddMonths(months);
        _endDate = _startDate.Value.AddDays(daysDiff);
    }

    private async Task PreviousMonth()
    {
        ShiftDateRange(-1);
        await LoadFinances();
    }

    private async Task NextMonth()
    {
        ShiftDateRange(1);
        await LoadFinances();
    }

    private async Task LoadFinances()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            var startOffset = new DateTimeOffset(_startDate!.Value.Date, TimeSpan.Zero);
            var endOffset = new DateTimeOffset(
                _endDate!.Value.Date.AddDays(1).AddTicks(-1),
                TimeSpan.Zero
            );

            var appointments = await AppointmentHandler.GetAllAppointmentsByDateRange(
                startOffset,
                endOffset,
                CancellationToken.None
            );

            var packages = await PackageHandler.GetAllPackagesByDateRange(
                startOffset,
                endOffset,
                CancellationToken.None
            );

            // 1. Packages Revenue
            _packagesRevenue = packages.Sum(p => p.Price);

            // 2. Realizada Revenue for Period (Completed Appointments + Packages)
            _realizedRevenue =
                appointments
                    .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Realizada)
                    .Sum(a => a.Price) + _packagesRevenue;

            // 3. Pending Revenue for Period (Scheduled Appointments)
            _pendingRevenue = appointments
                .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Marcada)
                .Sum(a => a.Price);

            // 4. Total Revenue for Period
            _totalRevenue = _realizedRevenue + _pendingRevenue;

            // 5. Monthly Revenue Chart Data (Bar) - Query the entire calendar year of the start date for full trend
            var yearStart = new DateTimeOffset(
                new DateTime(_startDate.Value.Year, 1, 1),
                TimeSpan.Zero
            );
            var yearEnd = new DateTimeOffset(
                new DateTime(_startDate.Value.Year, 12, 31, 23, 59, 59),
                TimeSpan.Zero
            );

            var chartAppointments = await AppointmentHandler.GetAllAppointmentsByDateRange(
                yearStart,
                yearEnd,
                CancellationToken.None
            );

            var chartPackages = await PackageHandler.GetAllPackagesByDateRange(
                yearStart,
                yearEnd,
                CancellationToken.None
            );

            var monthlyData = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                var monthlyAppointments = (double)
                    chartAppointments
                        .Where(a =>
                            a.AppointmentDate.Year == _startDate.Value.Year
                            && a.AppointmentDate.Month == i
                            && a.AppointmentStatus == AppointmentStatusEnum.Realizada
                        )
                        .Sum(a => a.Price);

                var monthlyPackages = (double)
                    chartPackages
                        .Where(p =>
                            p.CreatedAt.Year == _startDate.Value.Year && p.CreatedAt.Month == i
                        )
                        .Sum(p => p.Price);

                monthlyData[i - 1] = monthlyAppointments + monthlyPackages;
            }

            _monthlyRevenueLabels =
            [
                "Jan",
                "Fev",
                "Mar",
                "Abr",
                "Mai",
                "Jun",
                "Jul",
                "Ago",
                "Set",
                "Out",
                "Nov",
                "Dez",
            ];

            _monthlyRevenueSeries =
            [
                new ChartSeries<double>
                {
                    Name = $"Receita Realizada - {_startDate.Value.Year} (R$)",
                    Data = monthlyData,
                },
            ];

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading finances");
        }
        finally
        {
            _loading = false;
        }
    }

    #endregion
}
