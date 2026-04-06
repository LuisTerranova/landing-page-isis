using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace landing_page_isis.Components.Admin;

public partial class FinancesView : ComponentBase
{
    #region Services

    [Inject]
    private IAppointmentHandler AppointmentHandler { get; set; } = null!;

    [Inject]
    private IAppointmentPackageHandler PackageHandler { get; set; } = null!;

    #endregion

    #region Properties

    private bool _loading = true;

    // Filters
    private DateRange _dateRange = new DateRange(
        new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
        new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)
        )
    );

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
        await LoadFinances();
    }

    private async Task LoadFinances()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            var start = _dateRange?.Start ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
            var end = _dateRange?.End ?? new DateTime(DateTime.UtcNow.Year, 12, 31);

            var startOffset = new DateTimeOffset(start.Date, TimeSpan.Zero);
            var endOffset = new DateTimeOffset(end.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);

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
            _packagesRevenue = packages.Where(p => p != null).Sum(p => p!.Price);

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

            // 5. Monthly Revenue Chart Data (Bar)
            var monthlyData = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                var monthlyAppointments = (double)
                    appointments
                        .Where(a => a.AppointmentDate.Month == i && a.AppointmentStatus == AppointmentStatusEnum.Realizada)
                        .Sum(a => a.Price);

                var monthlyPackages = (double)
                    packages.Where(p => p.CreatedAt.Month == i).Sum(p => p.Price);

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
                new ChartSeries<double> { Name = "Receita Realizada (R$)", Data = monthlyData },
            ];

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading finances: {ex.Message}");
        }
        finally
        {
            _loading = false;
        }
    }

    #endregion
}
