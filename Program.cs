﻿using System.Threading.Tasks;
using CMS.Base;
using CMS.DataEngine;
using CMS.Membership;
using Hangfire;
using Kentico.Membership;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKentico();

// Add required Hangfire services.
builder.Services.AddHangfire(c =>
{
    // Configure SQL Server as a job storage.
    c.UseSqlServerStorage(
        // Obtain Kentico Database connection string.
        builder.Configuration.GetConnectionString(ConnectionHelper.ConnectionStringName));
});
// Add Hangfire Server which processes background jobs.
builder.Services.AddHangfireServer();

// Setup redirect path for unauthorized requests.
builder.Services.Configure<CookieAuthenticationOptions>(AdminIdentityConstants.APPLICATION_SCHEME, o =>
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

app.MapHangfireDashboard(new DashboardOptions
{
    Authorization = []
}).RequireAuthorization("XperienceAdministrationAccessPolicy");

app.Kentico().MapRoutes();
app.MapGet("/", () => "The XperienceByKentico site has not been configured yet.");

// Get IRecurringJobManager service from the application dependency injection container.
var manager = app.Services.GetRequiredService<IRecurringJobManager>();

// Configure desired schedule tasks. 
manager.AddOrUpdate("ScheduleTasks.TestJob", () => ScheduleTasks.TestJob(), Cron.Minutely());

app.Run();

class ScheduleTasks
{
    public static async Task TestJob()
    {
        ContextUtils.ResetCurrent();

        await UserInfo.Provider.Get().GetEnumerableTypedResultAsync();
    }
}