using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViscTronics.Zeitlib
{
    /// <summary>
    /// Basic calendar item class
    /// </summary>
    public class CalendarItem
    {
        public string Label { get; set; }
        public string Location { get; set; }

        public CalendarItem(string label, string location)
        {
            this.Label = label;
            this.Location = location;
        }
        
    }

    /// <summary>
    /// Defines a repeated weekly timetable calendar item.
    /// ie. Event occurrs every week on the specified day at the specified time
    /// </summary>
    public class WeeklyTimetableItem : CalendarItem
    {
        public DayOfWeek DayOfWeek { get; set; }
        public DateTime Time { get; set; }
        public uint Length { get; set; } // In minutes

        public WeeklyTimetableItem(string label, string location, DayOfWeek dayOfWeek, DateTime time, uint length) : base(label, location)
        {
            this.DayOfWeek = dayOfWeek;
            this.Time = time;
            this.Length = length;
        }
    }
}
