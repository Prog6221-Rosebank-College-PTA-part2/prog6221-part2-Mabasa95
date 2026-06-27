using System;
using System.Collections.Generic;

namespace Part_2
{
    public class TaskManager
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly ActivityLogManager _logManager;
        public string UserName { get; set; }

        public TaskManager(DatabaseHelper dbHelper, ActivityLogManager logManager)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
        }

        // Retrieves all tasks associated with the current user
        public List<TaskItem> GetTasks()
        {
            if (string.IsNullOrWhiteSpace(UserName)) return new List<TaskItem>();
            return _dbHelper.GetTasks(UserName);
        }

        // Creates a new task and records the activity in the log
        public void AddTask(string title, string description, DateTime? dueDate)
        {
            _dbHelper.AddTask(UserName, title, description, dueDate);
            _logManager.Log($"Created Task: '{title}'");
        }

        // Marks an existing task as completed and records the activity
        public void CompleteTask(int taskId, string taskTitle)
        {
            _dbHelper.CompleteTask(taskId);
            _logManager.Log($"Completed Task: '{taskTitle}'");
        }

        // Deletes a task and records the activity
        public void DeleteTask(int taskId, string taskTitle)
        {
            _dbHelper.DeleteTask(taskId);
            _logManager.Log($"Deleted Task: '{taskTitle}'");
        }

        // Processes chatbot requests to automatically create a new task
        public bool ParseAndAddTaskFromChat(string text, out string resultMessage)
        {
            string cleaned = text;
            DateTime? reminderDate = null;

            // Check the date
            if (text.Contains("tomorrow"))
            {
                reminderDate = DateTime.Now.AddDays(1);
                cleaned = text.Replace("tomorrow", "").Trim();
            }
            else if (text.Contains("in 3 days"))
            {
                reminderDate = DateTime.Now.AddDays(3);
                cleaned = text.Replace("in 3 days", "").Trim();
            }

            // Remove extra words
            string[] prefixes = { "add task", "add a task to", "remind me to" };
            foreach (var prefix in prefixes)
            {
                if (cleaned.StartsWith(prefix))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                }
            }

            if (string.IsNullOrEmpty(cleaned))
            {
                resultMessage = "I noticed that you'd like to add a task , but I couldn't identify the task details. Please try again using: 'add task [task details]'";
                return false;
            }

            string taskTitle = cleaned.Length > 30 ? cleaned.Substring(0, 30) : cleaned;
            string description = $"Created automatically from chatbot query: \"{text}\"";

            AddTask(taskTitle, description, reminderDate);

            resultMessage = $"Your task: '{taskTitle}' has been added successfully.{(reminderDate.HasValue ? $" A reminder has been scheduled for {reminderDate.Value:yyyy-MM-dd}." : "")}";
            return true;
        }
    }
}
