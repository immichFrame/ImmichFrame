namespace ImmichFrame.Core.Interfaces
{
    public interface IWeather
    {
        public double Temperature { get; set; }
        public string Unit { get; set; }
        public string TemperatureUnit { get; set; }
        public string Description { get; set; }
        public string IconId { get; set; }
    }
}
