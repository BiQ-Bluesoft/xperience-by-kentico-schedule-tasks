using Hangfire;

namespace Bluesoft.Xperience.ScheduleTasks;

public class ScheduleTaskOptions
{
    public IList<Action<IRecurringJobManager>> ConfigureRecurringJobs { get; } =
        new List<Action<IRecurringJobManager>>();
}