using System.Collections.Generic;

namespace Geco.Common
{
    /// <summary>
    /// Represents the task runner. Can be used by tasks to trigger running additional child tasks.
    /// </summary>
    public interface ITaskRunner
    {
        /// <summary>
        /// Runs the tasks with the given name and config object.
        /// If the config is <c>null</c> then the config task config from the configuration file is used
        /// </summary>
        void RunTask(string taskName, object config = null);
        /// <summary>
        /// Runs a list of tasks by name
        /// </summary>
        /// <param name="taskNames"></param>
        void RunTasks(IEnumerable<string> taskNames);
        /// <summary>
        /// Runs a list of tasks defined in the config file with the given task list name
        /// </summary>
        /// <param name="taskListName"></param>
        void RunNamedTaskList(string taskListName);
    } 
}