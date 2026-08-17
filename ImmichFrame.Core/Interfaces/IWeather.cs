namespace ImmichFrame.Core.Interfaces
{
    public interface IWeather
    {
        public string Location { get; set; }
        public double Temperature { get; set; }
        public double? MinimumTemperature { get; set; }
        public double? MaximumTemperature { get; set; }
        public double Humidity { get; set; }
        public string Unit { get; set; }
        public string TemperatureUnit { get; set; }
        public string Description { get; set; }
        public string IconId { get; set; }
    }
}
