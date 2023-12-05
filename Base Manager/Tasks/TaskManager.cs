using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript.Tasks
{
    class TaskManager
    {
        readonly Program _program;

        private readonly Dictionary<Task, Schedule> scheduledTasks = new Dictionary<Task, Schedule>();
        private readonly Queue<Task> tasksScheduledOnce = new Queue<Task>();

        public TaskManager(Program program)
        {
            _program = program;
        }

        public void Schedule(Task task, int frequency, int delay = 0)
        {
            scheduledTasks[task] = new Schedule(frequency, delay);
        }

        public void ScheduleOnce(Task task)
        {
            tasksScheduledOnce.Enqueue(task);
        }

        public void Run(int step)
        {
            if (tasksScheduledOnce.Count > 0)
            {
                tasksScheduledOnce.Dequeue()?.Run();
            }

            foreach (var scheduledTask in scheduledTasks)
            {
                var schedule = scheduledTask.Value;
                var task = scheduledTask.Key;
                schedule.Tick += step;
                if (Program.PRINT_DEBUG)
                {
                    _program.Echo($"[DEBUG]: {task.Name} tick: {schedule.Tick} / {schedule.TickToRunAt}");
                }

                if (schedule.Tick >= schedule.TickToRunAt)
                {
                    schedule.Tick = 0;
                    _program.Echo($"Running task: {task.Name}");
                    task.Run();
                }
            }
        }
    }

    class Schedule
    {
        public Schedule(int frequency, int delay = 0)
        {
            Tick = frequency - delay - 1;
            TickToRunAt = frequency;
        }

        public int Tick;
        public int TickToRunAt;
    }

    interface Task
    {
        string Id { get; }
        string Name { get; }

        void Run();
    }
}
