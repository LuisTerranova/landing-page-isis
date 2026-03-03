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

    #endregion

    #region Properties

    private bool _loading = true;

    // KPIs
    private decimal _currentRevenue;
    private decimal _expectedMonthlyRevenue;
    private decimal _annualRevenue;

    // Charts Data
    private double[] _statusChartData = [];
    private string[] _statusChartLabels = [];

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

            var appointments = await AppointmentHandler.GetAppointmentsByDateRange(
                startOfYear,
                endOfYear,
                CancellationToken.None
            );

            // 1. Annual Revenue (All "Realizada" in the year)
            _annualRevenue = appointments
                .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Realizada)
                .Sum(a => a.Price);

            // 2. Expected Monthly Revenue (All appointments in current month, regardless of status)
            _expectedMonthlyRevenue = appointments
                .Where(a => a.AppointmentDate.Month == currentMonth)
                .Sum(a => a.Price);

            // 3. Current Revenue (Only "Realizada")
            _currentRevenue = appointments
                .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Realizada)
                .Sum(a => a.Price);

            // 4. Status Chart Data (Donut)
            var statusGroups = appointments
                .GroupBy(a => a.AppointmentStatus)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToList();

            _statusChartLabels = statusGroups.Select(g => g.Status).ToArray();
            _statusChartData = statusGroups.Select(g => (double)g.Count).ToArray();

            // 5. Monthly Revenue Chart Data (Bar)
            var monthlyData = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                monthlyData[i - 1] = (double)
                    appointments
                        .Where(a =>
                            a.AppointmentDate.Month == i
                            && a.AppointmentStatus == AppointmentStatusEnum.Realizada
                        )
                        .Sum(a => a.Price);
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
