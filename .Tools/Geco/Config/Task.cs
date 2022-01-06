using System;
using System.Collections.Generic;

namespace Geco.Config
{
    public class TaskConfig
    {
        /// <summary>
        ///     Name of the task
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Class name of task to run
        /// </summary>
        public string TaskClass { get; set; }

        public bool OutputToConsole { get; set; }
        public string BaseOutputPath { get; set; }
        public string CleanFilesPattern { get; set; }
        internal string ConfigPath { get; set; }
        internal ConsoleColor Color { get; set; } = ConsoleColor.Blue;
        public List<TaskConfig> Tasks { get; set; } = new();
    }
}