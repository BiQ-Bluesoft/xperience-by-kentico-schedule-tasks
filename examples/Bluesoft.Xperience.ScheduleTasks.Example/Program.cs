using Bluesoft.Xperience.ScheduleTasks;

using CMS.Membership;

using Hangfire;

using Kentico.Membership;
using Kentico.Web.Mvc;

using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKentico();

// Add required Hangfire services.
builder.Services.AddHangfire(c =>
{
    c.UseXperienceSqlServerStorage(builder.Configuration);
    c.UseXperienceResetContextFilter();
});
// Add Hangfire Server which processes background jobs.
builder.Services.AddHangfireServer().AddHttpClient();

builder.Services.AddRecurringJob<ScheduleTasks>(nameof(ScheduleTasks));

// Setup redirect path for unauthorized requests.
builder.Services.Configure<CookieAuthenticationOptions>(
    AdminIdentityConstants.APPLICATION_SCHEME,
    o =>
    {
        o.LoginPath = "/admin/logon";
    });

builder.Services.AddAuthentication();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.InitKentico();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();

app.UseKentico();

app.UseAuthorization();

// Map Hangfire Dashboard UI and setup required authorization
app.MapHangfireDashboard(new DashboardOptions { Authorization = [] })
    .RequireXperienceAdministrationAccessPolicy();

app.Kentico().MapRoutes();
app.MapGet("/", () => "The XperienceByKentico site has not been configured yet.");

await app.RunAsync();

internal sealed class ScheduleTasks : IScheduleTask
{
    public async Task<object> Execute()
    {
        await UserInfo.Provider.Get().GetEnumerableTypedResultAsync();

        return "OK";
    }
}