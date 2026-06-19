using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.Configuration
{
    public class SchedulingSettings
    {
        public string RunType { get; set; } // Daily, Hourly, etc.
        public int? Hour { get; set; } // Hour for daily tasks
        public int? Minute { get; set; } = 0; // Minute for daily tasks
        public string DayOfWeek { get; set; } // For weekly tasks
        public DateTime? LastRunTime { get; set; } // Last run time for daily/weekly/monthly tasks
        public DateTime? LastRunTimeInterval { get; set; } // Last run time for interval-based tasks
        public bool RunImmediatelyOnStart { get; set; } // Flag to run immediately on start
        public int IntervalHours { get; set; } = 3; // Interval duration in hours for interval tasks
    }
}
