using ImmichFrame.Core.Models;

public interface ICustomWidgetService
{
    Task<List<CustomWidgetData>> GetCustomWidgetData();
}
