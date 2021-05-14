namespace Geco.Common
{
    /// <summary>
    /// A helper task that runs a list of other tasks. Can be used as a base class for composite tasks
    /// </summary>
    [Options(typeof(TaskListRunnerOptions))]
    public class TaskListRunner : IRunnable
    {
        private readonly TaskListRunnerOptions options;

        public TaskListRunner(ITaskRunner taskRunner, TaskListRunnerOptions options)
        {
            this.TaskRunner = taskRunner;
            this.options = options;
        }

        protected ITaskRunner TaskRunner { get; }

        public void Run()
        {
            Run(options);
        }

        protected virtual void Run(TaskListRunnerOptions taskList)
        {
            TaskRunner.RunTasks(options);
        }
    }
}