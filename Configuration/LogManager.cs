using System;
using System.IO;

namespace SageIntegration.Configuration
{
    public sealed class LogManager
    {
        private static readonly Lazy<LogManager> _instance = new Lazy<LogManager>(() => new LogManager());
        private static readonly object _lock = new object();

        private string _logDirectory;
        private string _currentDate;

        // Private constructor to prevent instantiation
        private LogManager()
        {
            UpdateLogDirectory();
        }

        // Public property to access the instance
        public static LogManager Instance => _instance.Value;

        // Method to log messages with optional subfolder
        public void LogMessage(string message, string subfolder = null)
        {
            lock (_lock)
            {
                UpdateLogDirectoryIfDateChanged();
                var filePath = GetFilePath("Log.txt", subfolder);
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                File.AppendAllText(filePath, logEntry);
            }
        }

        // Method to log exceptions with optional subfolder
        public void LogException(Exception ex, string subfolder = null)
        {
            lock (_lock)
            {
                UpdateLogDirectoryIfDateChanged();
                var filePath = GetFilePath("Exception.txt", subfolder);
                var exceptionEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {ex.Message}\n{ex.StackTrace}\n";
                File.AppendAllText(filePath, exceptionEntry);
            }
        }

        // Helper method to set the main directory path based on the current date
        private void UpdateLogDirectory()
        {
            _currentDate = DateTime.Now.ToString("yyyy-MM-dd");

            // Set the base directory for the logs relative to the app's base directory
            string logBaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            _logDirectory = Path.Combine(logBaseDirectory, _currentDate);

            // Create the directory if it does not exist
            Directory.CreateDirectory(_logDirectory);
        }

        // Update directory if the date has changed (e.g., on the next day)
        private void UpdateLogDirectoryIfDateChanged()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_currentDate != today)
            {
                UpdateLogDirectory();
            }
        }

        // Helper method to get the full file path for logs/exceptions with optional subfolder
        private string GetFilePath(string fileName, string subfolder)
        {
            var directory = string.IsNullOrWhiteSpace(subfolder)
                ? _logDirectory
                : Path.Combine(_logDirectory, subfolder);

            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }
    }
}
