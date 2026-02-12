using landing_page_isis;
using landing_page_isis.Components;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

//Force http, omarchy's fault perhaps.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
  serverOptions.ListenLocalhost(5000, listenOptions =>
  {
  });
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<IsisTheme>();
builder.Services.AddScoped<IAppointmentHandler, AppointmentHandler>();
builder.Services.AddScoped<IPacientHandler, PacientHandler>();
builder.Services.AddScoped<ILeadHandler, LeadHandler>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
      options.LoginPath = "/login";
      options.AccessDeniedPath = "/acesso-negado";
      options.ExpireTimeSpan = TimeSpan.FromDays(3);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.SeedAdmin();
app.Run();
