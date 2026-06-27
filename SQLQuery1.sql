-- Create tasks table
CREATE TABLE tasks (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(100) NOT NULL,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    reminder_date DATETIME NULL,
    is_completed BIT DEFAULT 0 NOT NULL
);

-- Create activity_logs table
CREATE TABLE activity_logs (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(100) NOT NULL,
    action_description NVARCHAR(255) NOT NULL,
    timestamp DATETIME DEFAULT GETDATE() NOT NULL
);