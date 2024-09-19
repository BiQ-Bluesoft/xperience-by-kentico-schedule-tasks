using CMS.DataEngine;

using Hangfire;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bluesoft.Xperience.ScheduleTasks;

public static class HangfireRecurringJobsExtensions
{
    private const string XperienceAdministrationAccessPolicy = "XperienceAdministrationAccessPolicy";

    public static void RequireXperienceAdministrationAccessPolicy(this IEndpointConventionBuilder endpoint)
    {
        endpoint.RequireAuthorization(XperienceAdministrationAccessPolicy);
    }

    public static void UseXperienceResetContextFilter(this IGlobalConfiguration configuration)
    {
        configuration.UseFilter(new ResetXperienceContextFilter());
    }

    public static void UseXperienceSqlServerStorage(this IGlobalConfiguration configuration,
        IConfiguration builderConfiguration)
    {
        configuration.UseSqlServerStorage(
            builderConfiguration.GetConnectionString(ConnectionHelper.ConnectionStringName));
    }

    public static void AddRecurringJob<T>(this IServiceCollection serviceCollection,
        string recurringJobId,
        RecurringJobOptions? options = null,
        string hangfireConfigurationSectionName = "Hangfire",
        string queue = "default")
        where T : class, IScheduleTask
    {
        serviceCollection.TryAddTransient<T>();
        serviceCollection.AddOptions<ScheduleTaskOptions>()
            .Configure<IConfiguration>((o, c) =>
                o.ConfigureRecurringJobs.Add(
                    manager => manager.AddRecurringJob<T>(
                        recurringJobId,
                        queue,
                        c.GetSection(hangfireConfigurationSectionName).GetValue<string?>(recurringJobId) ?? Cron.Never(),
                        options)));


        EnsureStartupFilterExists(serviceCollection);
    }

    public static void AddRecurringJob<T>(this IServiceCollection serviceCollection,
        string recurringJobId,
        string cron,
        RecurringJobOptions? options = null,
        string queue = "default")
        where T : class, IScheduleTask
    {
        serviceCollection.TryAddTransient<T>();
        serviceCollection.AddOptions<ScheduleTaskOptions>()
            .Configure(o =>
                o.ConfigureRecurringJobs.Add(
                    manager => manager.AddRecurringJob<T>(recurringJobId, queue, cron, options)));


        EnsureStartupFilterExists(serviceCollection);
    }

    private static readonly ServiceDescriptor MarkerService =
        ServiceDescriptor.Singleton<HangfireStartupFilterMarker, HangfireStartupFilterMarker>();

    private static void EnsureStartupFilterExists(IServiceCollection serviceCollection)
    {
        if (serviceCollection.Contains(MarkerService))
        {
            return;
        }

        serviceCollection.Add(MarkerService);
        serviceCollection.AddSingleton<IStartupFilter, HangfireStartupFilter>();
    }

#pragma warning disable S2094
    private sealed class HangfireStartupFilterMarker;
#pragma warning restore S2094

    private sealed class HangfireStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var options = app.ApplicationServices.GetRequiredService<IOptions<ScheduleTaskOptions>>();
                var recurringJobManager = app.ApplicationServices.GetRequiredService<IRecurringJobManager>();

                foreach (var configureRecurringJob in options.Value.ConfigureRecurringJobs)
                {
                    configureRecurringJob(recurringJobManager);
                }

                next(app);
            };
        }
    }

    private static void AddRecurringJob<T>(this IRecurringJobManager recurringJobManager,
        string recurringJobId,
        string queue,
        string cron,
        RecurringJobOptions? options)
        where T : IScheduleTask

    {
        recurringJobManager.AddOrUpdate<T>(recurringJobId,
            queue,
            t => t.Execute(),
            cron,
            options);
    }
}