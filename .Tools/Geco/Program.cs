// Copyright © iQuarc 2017 - Pop Catalin Sever
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Geco.Common;
using Geco.Common.Util;
using Geco.Config;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Geco.Common.Util.ColorConsole;
using static System.ConsoleColor;

namespace Geco
{
    /// <summary>
    ///     As simple as it gets code generator, which is a console application that runs code generation tasks written in C#.
    /// </summary>
    /// <remarks>
    ///     Task discovery is done at runtime by scanning current assembly for all the types that implement
    ///     <see cref="IRunnable" /> interfaces.
    ///     The tasks are resolved using a <see cref="ServiceProvider" />. Generator tasks can declare a options class using
    ///     the <see cref="OptionsAttribute" />
    ///     in order to have the options be read from the <c>appsettings.json</c> configuration file and registered in the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollection" />
    /// </remarks>
    public class Program : ITaskRunner
    {
        private const int TaskMaxNestingLevel = 50;
        private IConfigurationRoot configurationRoot;
        private int nestingLevel;
        private RootConfig rootConfig;
        private Dictionary<string, Type> runnableTypes;
        private IServiceCollection serviceCollection;
        public bool Interactive { get; private set; }


        private static int Main(string[] args)
        {
            var p = new Program();
            return p.Run(args);
        }

        private int Run(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                var app = new CommandLineApplication(false);
                app.Name = "Geco";
                app.HelpOption("-?|-h|--help");

                app.Command("run", command =>
                {
                    command.HelpOption("-?|-h|--help");
                    var taskList = command.Option("-tl|--tasklist"
                        , "The name of the task list from appsettings.json to execute. The task list is an array of task names.",
                        CommandOptionType.SingleValue);
                    var taskNames = command.Option("-tn|--taskname <taskname>"
                        , "The name(s) of the tasks to execute. The task names is an list of task names parameters -tn <xx> -tn <yy>.",
                        CommandOptionType.MultipleValue);
                    command.OnExecute(() =>
                    {
                        ConfigureServices(app.RemainingArguments.ToArray());
                        if (taskList.HasValue())
                            RunTaskListFromConfig(taskList.Value());
                        if (taskNames.HasValue())
                            RunTasksList(taskNames.Values);
                        return 0;
                    });
                });
                app.OnExecute(() =>
                {
                    WriteLogo();
                    ConfigureServices(app.RemainingArguments.ToArray());
                    WriteLine($"<< Geco is running in interactive mode! >>", Yellow);
                    InteractiveLoop();
                    WriteLine($"C ya!", Yellow);
                    return 0;
                });

                return app.Execute(args);
            }
            catch (Exception ex)
            {
                WriteLine($"==============================================", Red);
                WriteLine($"=== Geco stopped due to error:", Red);
                WriteLine($"{ex}", Yellow);
                WriteLine($"==============================================", Red);
                return -1;
            }
        }

        private static void WriteLogo()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            WriteLine($"********************************************************", Blue);
            WriteLine($"* ** Geco v{version} **                                  *", Blue);
            WriteLine($"*                                                      *", Blue);
            WriteLine(("*", Blue), (@"        .)/     )/,         ", Green), ("        Copyright (c)     *", Blue));
            WriteLine(("*", Blue), (@"         /`-._,-'`._,@`-,   ", Green), ("         iQuarc 2017      *", Blue));
            WriteLine(("*", Blue), (@"  ,  _,-=\,-.__,-.-.__@/    ", Green), ("    - Generator Console - *", Blue));
            WriteLine(("*", Blue), (@" (_,'    )\`    '(`         ", Green), ("           - Geco -       *", Blue));
            WriteLine($"*                                                      *", Blue);
            WriteLine($"*          {("https://github.com/iQuarc/Geco.Core", DarkMagenta)}         *", Blue);
            WriteLine($"********************************************************", Blue);
        }

        private void InteractiveLoop()
        {
            Interactive = true;
            Func<bool> displayAndRun;
            do
            {
                displayAndRun = BuildMenu();
            } while (displayAndRun());
        }

        private Func<bool> BuildMenu()
        {
            Dictionary<string, Action> BuildMenuRecursive(IReadOnlyCollection<TaskConfig> tasks, string path)
            {
                Console.WriteLine();
                WriteLine($"Path: {(path, Yellow)}", DarkCyan);
                WriteLine($"Select option {("(then press Enter)", Gray)}:", White);
                var actions = new Dictionary<string, Action>();

                foreach (var taskInfo in tasks.WithInfo())
                {
                    var taskNr = (taskInfo.Index + 1).ToString();
                    WriteLine(($"{taskNr}. ", White), ($"{taskInfo.Item.Name}", taskInfo.Item.Color));

                    actions.Add(taskNr, () =>
                    {
                        if (taskInfo.Item.TaskClass != null)
                        {
                            this.RunningContext.Path = path;
                            RunTask(taskInfo.Item);
                        } 
                        else
                        if (taskInfo.Item.Tasks.Count > 0)
                        {
                            Dictionary<string, Action> subActions;
                            do
                            {
                                subActions = BuildMenuRecursive(taskInfo.Item.Tasks, path + $"{taskInfo.Item.Name}/");
                                WriteLine(("q. ", White), ("Back", Yellow));
                                Write($">>", White);
                            } while (Choose(subActions));
                        }
                    });
                }

                return actions;
            }

            var act = BuildMenuRecursive(rootConfig.Tasks, "/");
            WriteLine(("q. ", White), ("Quit", Yellow));
            Write($">>", White);

            bool Choose(Dictionary<string, Action> actions)
            {
                var command = Console.ReadLine().Trim();
                if (string.Equals(command, "q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    return false;
                }

                if (actions.TryGetValue(command, out var action))
                    action();
                else 
                    WriteLine($"Bad command or file name", Yellow);
                return true;
            }

            return () => Choose(act);
        }

        public string PickOption(IEnumerable<(string Value, string Text)> questions)
        {
            WriteLine($"Path: {(RunningContext.Path + $"{RunningContext.TaskInfo.Name}/", Yellow)}", DarkCyan);
            WriteLine($"Select option {("(then press Enter)", Gray)}:", White);

            var dict = questions.WithInfo().ToDictionary(x => (x.Index + 1).ToString());

            foreach (var questionInfo in questions.WithInfo())
            {
                var taskNr = (questionInfo.Index + 1).ToString();
                WriteLine(($"{taskNr}. ", White), ($"{questionInfo.Item.Text}", Green));
            }

            WriteLine(("q. ", White), ("Quit", Yellow));
            Write($">>", White);

            var command = Console.ReadLine().Trim();
            if (string.Equals(command, "q", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                return null;
            }

            if (dict.ContainsKey(command))
                return dict[command].Item.Value;

            return PickOption(questions);
        }

        private void ConfigureServices(string[] args)
        {
            configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsetings.json.user", optional:true)
                .AddCommandLine(args)
                .Build();

            //setup the DI
            serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddSingleton(configurationRoot)
                .AddSingleton<ITaskRunner>(this)
                .AddOptions();
            ScanTasks();
            ScanServices();
        }

        private void ScanTasks()
        {
            rootConfig = new RootConfig();
            configurationRoot.Bind(rootConfig);

            runnableTypes = Assembly.GetAssembly(typeof(Program))
                .GetTypes()
                .Where(t => typeof(IRunnable).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.GetConstructors().Any())
                .ToDictionary(t => t.FullName);

            foreach (var runnableType in runnableTypes.Values)
                serviceCollection.Add(new ServiceDescriptor(runnableType, runnableType, ServiceLifetime.Transient));

            ScanTasksRecursive(rootConfig.Tasks);

            void ScanTasksRecursive(IReadOnlyCollection<TaskConfig> tasks, string parentPath = "")
            {
                //RootConfig rootConfig
                foreach (var taskConfig in tasks.WithInfo())
                {
                    if (taskConfig.Item.TaskClass != null && !runnableTypes.ContainsKey(taskConfig.Item.TaskClass))
                    {
                        WriteLine($"Task configuration for:[{taskConfig.Item.TaskClass}] has no corresponding service to be applied to", DarkYellow);
                        continue;
                    }

                    var currentPath = $"{parentPath}Tasks:{taskConfig.Index}:";

                    if (taskConfig.Item.TaskClass != null)
                    {
                        var taskType = runnableTypes[taskConfig.Item.TaskClass];
                        var optionsAttribute = (OptionsAttribute)taskType.GetCustomAttribute(typeof(OptionsAttribute));
                        if (optionsAttribute != null)
                            taskConfig.Item.ConfigPath = $"{currentPath}Options";

                        var colorAttribute = (ConsoleColorAttribute)taskType.GetCustomAttribute(typeof(ConsoleColorAttribute));
                        if (colorAttribute != null)
                            taskConfig.Item.Color = colorAttribute.ConsoleColor;
                    }

                    ScanTasksRecursive(taskConfig.Item.Tasks, currentPath);
                }
            }
        }

        public void ScanServices()
        {
            var serviceTypes = Assembly.GetAssembly(typeof(Program))!
                .GetTypes()
                .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<ServiceAttribute>() })
                .Where(x => x.Attribute?.ContractType != null && !x.Type.IsAbstract && !x.Type.IsGenericTypeDefinition && x.Type.GetConstructors().Any());
            foreach (var serviceType in serviceTypes)
                serviceCollection.Add(new ServiceDescriptor(serviceType.Attribute.ContractType, serviceType.Type, serviceType.Attribute.Lifetime));
        }

        private void RunTaskListFromConfig(string taskListName)
        {
            var taskList = new List<string>();
            configurationRoot.Bind(taskListName, taskList);
            RunTasksList(taskList);
        }

        private void RunTasksList(IEnumerable<string> taskList)
        {
            foreach (var taskName in taskList)
            {
                var task = FindTask(rootConfig.Tasks, taskName);
                if (task == null)
                {
                    WriteLine(("*** Error: Task ", Red), ($" {taskName} ", Blue), ("not found!", Red));
                    break;
                }
                task.OutputToConsole = false;
                if (!RunTask(task))
                    break;
            }
        }

        private TaskConfig FindTask(IReadOnlyCollection<TaskConfig> tasks, string taskName)
        {
            var task = tasks.FirstOrDefault(t => t.Name == taskName);

            if (task != null)
                return task;

            foreach (var t in tasks)
            {
                if (t.Tasks.Count > 0)
                    task = FindTask(t.Tasks, taskName);

                if (task != null)
                    return task;
            }

            return null;
        }

        private bool RunTask(TaskConfig itemInfo, object options = null)
        {
            var taskError = false;
            var sw = new Stopwatch();
            try
            {
                if (nestingLevel < 1)
                {
                    WriteLine($"--------------------------------------------------------", Yellow);
                    WriteLine(("*** Starting ", Yellow), ($" {itemInfo.Name} ", itemInfo.Color));
                }
                else
                    WriteLine(($"**** {new string('*', nestingLevel)} Starting child", Green), ($" {itemInfo.Name} ", itemInfo.Color));

                var taskType = runnableTypes[itemInfo.TaskClass];
                var optionsAttribute = (OptionsAttribute)taskType.GetCustomAttribute(typeof(OptionsAttribute));
                if (optionsAttribute != null && options == null)
                {
                    options = Activator.CreateInstance(optionsAttribute.OptionType);
                    configurationRoot.GetSection(itemInfo.ConfigPath).Bind(options);
                    serviceCollection.Replace(new ServiceDescriptor(optionsAttribute.OptionType, options));
                }

                if (options != null) serviceCollection.Replace(new ServiceDescriptor(options.GetType(), options));

                using (var provider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }))
                using (var scope = provider.CreateScope())
                {
                    var task = (IRunnable)scope.ServiceProvider.GetRequiredService(taskType);
                    if (task is IOutputRunnable to)
                    {
                        to.OutputToConsole = itemInfo.OutputToConsole;
                        to.BaseOutputPath = itemInfo.BaseOutputPath;
                        to.CleanFilesPattern = itemInfo.CleanFilesPattern;
                        to.Interactive = Interactive;
                    }

                    if (task is IRunnableConfirmation co && Interactive)
                    {
                        sw.Stop();
                        if (!co.GetUserConfirmation()) WriteLine(("*** Task was canceled ", Yellow), ($" {itemInfo.Name} ", Blue));
                    }

                    try
                    {

                        if (Interlocked.Increment(ref nestingLevel) <= TaskMaxNestingLevel)
                        {
                            this.RunningContext.Task     = task;
                            this.RunningContext.TaskInfo = itemInfo;
                            sw.Start();
                            task.Run();
                        }
                        else
                            WriteLine($"Error running {(itemInfo.Name, Blue)}: Error:{($"Maximum Task nesting level of {TaskMaxNestingLevel} was exceeded", Red)}", DarkRed);
                    }
                    catch (OperationCanceledException)
                    {
                        WriteLine(("*** Task was aborted ", Yellow), ($" {itemInfo.Name} ", Blue));
                        taskError = true;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref nestingLevel);
                        sw.Stop();
                    }
                }
            }
            catch (Exception ex) when (Interactive)
            {
                WriteLine($"Error running {(itemInfo.Name, Blue)}: Error:{(ex.Message, Red)}", DarkRed);
                WriteLine($"Detail: {ex}", DarkYellow);
                taskError = true;
            }

            WriteLine();
            WriteLine(("Task", Yellow), ($" {itemInfo.Name} ", Blue), ("completed", Green), (" in", Yellow), ($" {sw.ElapsedMilliseconds} ms", Green));
            return !taskError;
        }

        private RunningTaskContext RunningContext { get; } = new();

        private class RunningTaskContext
        {
            public IRunnable Task { get; set; }
            public TaskConfig TaskInfo { get; set; }
            public string Path { get; set; }
        }

        void ITaskRunner.RunTask(string taskName, object config)
        {
            var task = FindTask(rootConfig.Tasks, taskName);
            if (task == null)
            {
                WriteLine(("*** Error: Task ", Red), ($" {taskName} ", Blue), ("not found!", Red));
                return;
            }

            RunTask(task, config);
        }

        void ITaskRunner.RunTasks(IEnumerable<string> taskNames)
        {
            RunTasksList(taskNames);
        }

        void ITaskRunner.RunNamedTaskList(string taskListName)
        {
            RunTaskListFromConfig(taskListName);
        }
    }
}