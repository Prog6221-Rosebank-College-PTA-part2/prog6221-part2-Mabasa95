using System;
using System.Collections.Generic;

namespace Part_2
{
    public class ActivityLogManager
    {
        private readonly DatabaseHelper _dbHelper;
        public string UserName { get; set; }
        public int CurrentLogLimit { get; private set; } = 5;

        public ActivityLogManager(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        // Records a new user activity in the database
        public void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(UserName)) return;
            _dbHelper.AddLog(UserName, message);
        }

        // Retrieves the user's activity logs up to the current display limit
        public List<string> GetLogs()
        {
            if (string.IsNullOrWhiteSpace(UserName)) return new List<string>();
            return _dbHelper.GetLogs(UserName, CurrentLogLimit);
        }

        // Increases the number of displayed activity logs and returns the updated list
        public List<string> ShowMoreLogs()
        {
            CurrentLogLimit += 10;
            return GetLogs();
        }

        // Records the activity log display limit to the default value
        public void ResetLimit()
        {
            CurrentLogLimit = 5;
        }
    }
}