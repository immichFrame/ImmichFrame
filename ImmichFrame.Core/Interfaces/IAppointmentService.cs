using ImmichFrame.Core.Interfaces;

public interface ICalendarService
{
    public Task<List<IAppointment>> GetAppointments();
}