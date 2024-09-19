namespace Bluesoft.Xperience.ScheduleTasks;

public interface IScheduleTask
{
    Task<object> Execute();
}