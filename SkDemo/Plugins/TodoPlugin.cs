using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SkDemo.Plugins
{
    public class TodoPlugin
    {
        private static readonly List<string> _tasks = new();

        [KernelFunction, Description("Task description to add")]
        public Task<string> AddTaskAsync(string task)
        {
            _tasks.Add(task);
            return Task.FromResult($"Added task: {task}");
        }

        [KernelFunction, Description("Task description to remove")]
        public Task<string> RemoveTaskAsync(string task)
        {
            if (_tasks.Remove(task))
                return Task.FromResult($"Removed task: {task}");

            return Task.FromResult($"Task not found: {task}");
        }

        [KernelFunction, Description("List all tasks")]
        public Task<string> ListTasksAsync(string input)
        {
            return Task.FromResult(
                _tasks.Count == 0
                    ? "No tasks available"
                    : string.Join("; ", _tasks)
            );
        }
    }
}