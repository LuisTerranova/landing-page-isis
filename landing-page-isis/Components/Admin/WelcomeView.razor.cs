using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using Microsoft.AspNetCore.Components;

namespace landing_page_isis.Components.Admin;

public partial class WelcomeView : ComponentBase
{
    [Inject] private IAppointmentHandler AppointmentHandler { get; set; } = null!;
    
    private List<AppointmentViewModel> _appointments = new();
    private bool _loading = true;
    private ViewMode _currentView = ViewMode.Day;
    private DateTime _selectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        _loading = true;
        StateHasChanged();
        try
        {
            DateTimeOffset start, end;

            if (_currentView == ViewMode.Day)
            {
                var date = _selectedDate.Date;
                start = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                end = DateTime.SpecifyKind(date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            }
            else
            {
                int diff = (7 + (_selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                var monday = _selectedDate.AddDays(-1 * diff).Date;
                start = DateTime.SpecifyKind(monday, DateTimeKind.Utc);
                end = DateTime.SpecifyKind(monday.AddDays(7).AddTicks(-1), DateTimeKind.Utc);
            }

            var results = await AppointmentHandler.GetAppointmentsByDateRange(start, end, default);
            
            if (!results.Any())
            {
                // Injecting Mock Data for UI Testing
                _appointments = new List<AppointmentViewModel>
                {
                    new() { Id = Guid.NewGuid(), Date = new DateTimeOffset(_selectedDate.Date.AddHours(9)), PatientName = "Ana Beatriz", Status = AppointmentStatusEnum.Realizada, Price = 150.00m },
                    new() { Id = Guid.NewGuid(), Date = new DateTimeOffset(_selectedDate.Date.AddHours(10).AddMinutes(30)), PatientName = "Carlos Eduardo", Status = AppointmentStatusEnum.Marcada, Price = 200.00m },
                    new() { Id = Guid.NewGuid(), Date = new DateTimeOffset(_selectedDate.Date.AddHours(14)), PatientName = "Mariana Silva", Status = AppointmentStatusEnum.Marcada, Price = 180.00m },
                    new() { Id = Guid.NewGuid(), Date = new DateTimeOffset(_selectedDate.Date.AddHours(16)), PatientName = "Ricardo Oliveira", Status = AppointmentStatusEnum.Cancelada, Price = 150.00m }
                };
                
                if (_currentView == ViewMode.Week)
                {
                    // Add some variety for week view
                    _appointments.Add(new() { Id = Guid.NewGuid(), Date = new DateTimeOffset(_selectedDate.Date.AddDays(1).AddHours(11)), PatientName = "Juliana Costa", Status = AppointmentStatusEnum.Marcada, Price = 180.00m });
                    _appointments.Add(new() { Id = Guid.NewGuid(), Date = new DateTimeOffset(_selectedDate.Date.AddDays(2).AddHours(15)), PatientName = "Fernando Souza", Status = AppointmentStatusEnum.Marcada, Price = 220.00m });
                }
            }
            else
            {
                _appointments = results.Select(a => new AppointmentViewModel
                {
                    Id = a.Id,
                    Date = a.AppointmentDate,
                    PatientName = a.Pacient?.Name ?? "N/A",
                    Status = a.AppointmentStatus,
                    Price = a.Price
                }).OrderBy(a => a.Date).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading appointments: {ex.Message}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task ToggleView(ViewMode mode)
    {
        if (_currentView == mode) return;
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
        
        int diff = (7 + (_selectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = _selectedDate.AddDays(-1 * diff).Date;
        var sunday = monday.AddDays(6);
        return $"{monday:dd/MM} - {sunday:dd/MM}";
    }

    private enum ViewMode
    {
        Day,
        Week
    }

    public class AppointmentViewModel
    {
        public Guid Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public string PatientName { get; set; } = "";
        public AppointmentStatusEnum Status { get; set; }
        public decimal Price { get; set; }
    }
}
