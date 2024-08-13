using System;
using CMS.DataEngine;
using Hangfire;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKentico();

builder.Services.AddHangfire(c =>
{
    c.UseSqlServerStorage(
        builder.Configuration.GetConnectionString(ConnectionHelper.ConnectionStringName));
});
builder.Services.AddHangfireServer();

builder.Services.AddAuthentication();

builder.Services.AddControllersWithViews();

var app = builder.Build();

var manager = app.Services.GetRequiredService<IRecurringJobManager>();

manager.AddOrUpdate("TestJob", () => Console.WriteLine("TestJob"), Cron.Minutely());

app.InitKentico();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();

app.UseKentico();

app.MapHangfireDashboard();
app.Kentico().MapRoutes();
app.MapGet("/", () => "The XperienceByKentico site has not been configured yet.");

app.Run();
