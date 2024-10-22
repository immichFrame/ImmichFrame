namespace ImmichFrame.Core.Interfaces
{
    public interface IAppointment
    {
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime EndTime { get; set; }
        public string Summary { get; set; }
        public string Location { get; set; }
    }
}
