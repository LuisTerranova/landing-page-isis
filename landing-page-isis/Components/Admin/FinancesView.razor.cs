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

    // KPIs
    private decimal _currentRevenue;
    private decimal _expectedMonthlyRevenue;
    private decimal _annualRevenue;
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
        try
        {
            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            // Start of year to end of year
            var startOfYear = new DateTimeOffset(currentYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var endOfYear = new DateTimeOffset(currentYear, 12, 31, 23, 59, 59, TimeSpan.Zero);

            var appointments = await AppointmentHandler.GetAllAppointmentsByDateRange(
                startOfYear,
                endOfYear,
                CancellationToken.None
            );

            var packages = await PackageHandler.GetAllPackagesByDateRange(
                startOfYear,
                endOfYear,
                CancellationToken.None
            );

            // 0. Packages Revenue (Total strictly from packages this year)
            _packagesRevenue = packages.Where(p => p != null).Sum(p => p!.Price);

            // 1. Annual Revenue (All "Realizada" appointments + All Packages in the year)
            _annualRevenue = appointments
                .Where(a => a != null && a.AppointmentStatus == AppointmentStatusEnum.Realizada)
                .Sum(a => a!.Price) +
                packages.Where(p => p != null).Sum(p => p!.Price);

            // 2. Expected Monthly Revenue (All appointments in current month + Current Month Packages)
            _expectedMonthlyRevenue = appointments
                .Where(a => a != null && a.AppointmentDate.Month == currentMonth)
                .Sum(a => a!.Price) +
                packages.Where(p => p != null && p.CreatedAt.Month == currentMonth).Sum(p => p!.Price);

            // 3. Current Revenue (Only "Realizada" + All Packages)
            _currentRevenue = appointments
                .Where(a => a != null && a.AppointmentStatus == AppointmentStatusEnum.Realizada)
                .Sum(a => a!.Price) +
                _packagesRevenue;

            // 4. Monthly Revenue Chart Data (Bar)
            var monthlyData = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                var monthlyAppointments = (double)appointments
                        .Where(a =>
                            a != null &&
                            a.AppointmentDate.Month == i
                            && a.AppointmentStatus == AppointmentStatusEnum.Realizada
                        )
                        .Sum(a => a!.Price);

                var monthlyPackages = (double)packages
                        .Where(p => p != null && p.CreatedAt.Month == i)
                        .Sum(p => p!.Price);

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

            StateHasChanged(); // Force chart refresh after data is ready
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
