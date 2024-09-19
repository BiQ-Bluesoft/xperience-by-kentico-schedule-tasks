using CMS.Base;

using Hangfire.Server;

namespace Bluesoft.Xperience.ScheduleTasks;

internal sealed class ResetXperienceContextFilter : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        ContextUtils.ResetCurrent();
    }

    public void OnPerformed(PerformedContext context)
    {
    }
}