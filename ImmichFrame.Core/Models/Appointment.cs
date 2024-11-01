using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Models
{
    public class Appointment : IAppointment
    {
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime EndTime { get; set; }
        public string Summary { get; set; } = "";
        public string Description { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
