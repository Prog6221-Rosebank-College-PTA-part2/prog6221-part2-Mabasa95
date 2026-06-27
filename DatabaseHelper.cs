using System;
using System.Collections.Generic;
using System.Data.SqlClient; // // SQL Server library

namespace Part_2
{
    public class DatabaseHelper
    {
        // Database connection
        private readonly string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=cybersecurity_db;Integrated Security=True;";

        // Server connection
        private readonly string serverConnectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;";

        // Set up the database
        public void InitializeDatabase()
        {
            // Create the database
            using (var conn = new SqlConnection(serverConnectionString))
            {
                conn.Open();
                string createDbQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'cybersecurity_db')
                    BEGIN
                        CREATE DATABASE cybersecurity_db;
                    END;";
                using (var cmd = new SqlCommand(createDbQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // Create the tables
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Create the tasks table
                string createTasksTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tasks]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE tasks (
                            id INT IDENTITY(1,1) PRIMARY KEY,
                            username NVARCHAR(100) NOT NULL,
                            title NVARCHAR(255) NOT NULL,
                            description NVARCHAR(MAX),
                            reminder_date DATETIME NULL,
                            is_completed BIT DEFAULT 0 NOT NULL
                        );
                    END;";
                using (var cmd = new SqlCommand(createTasksTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Create the activity log table
                string createLogsTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[activity_logs]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE activity_logs (
                            id INT IDENTITY(1,1) PRIMARY KEY,
                            username NVARCHAR(100) NOT NULL,
                            action_description NVARCHAR(255) NOT NULL,
                            timestamp DATETIME DEFAULT GETDATE() NOT NULL
                        );
                    END;";
                using (var cmd = new SqlCommand(createLogsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Task methods

        public void AddTask(string username, string title, string description, DateTime? reminderDate)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO tasks (username, title, description, reminder_date) VALUES (@username, @title, @description, @reminderDate)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                    cmd.Parameters.AddWithValue("@reminderDate", (object)reminderDate ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TaskItem> GetTasks(string username)
        {
            var tasks = new List<TaskItem>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, title, description, reminder_date, is_completed FROM tasks WHERE username = @username";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskItem
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Title = reader.GetString(reader.GetOrdinal("title")),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                                ReminderDate = reader.IsDBNull(reader.GetOrdinal("reminder_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("reminder_date")),
                                IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed"))
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        public void CompleteTask(int taskId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE tasks SET is_completed = 1 WHERE id = @id"; // Mark task as completed
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTask(int taskId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM tasks WHERE id = @id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Activity log methods

        public void AddLog(string username, string description)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO activity_logs (username, action_description) VALUES (@username, @description)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@description", description);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<string> GetLogs(string username, int limit)
        {
            var logs = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Get the latest logs
                string query = "SELECT TOP (@limit) action_description, timestamp FROM activity_logs WHERE username = @username ORDER BY timestamp DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@limit", limit);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string desc = reader.GetString(reader.GetOrdinal("action_description"));
                            DateTime ts = reader.GetDateTime(reader.GetOrdinal("timestamp"));
                            logs.Add($"[{ts:yyyy-MM-dd HH:mm:ss}] {desc}");
                        }
                    }
                }
            }
            return logs;
        }
    }
    // Task class
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
    }
}