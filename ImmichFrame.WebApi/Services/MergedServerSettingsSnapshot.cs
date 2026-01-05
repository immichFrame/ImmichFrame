using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Services;

internal sealed class MergedServerSettingsSnapshot : IServerSettings
{
    public required IGeneralSettings GeneralSettings { get; init; }
    public required IEnumerable<IAccountSettings> Accounts { get; init; }

    public void Validate()
    {
        GeneralSettings.Validate();
        foreach (var account in Accounts)
        {
            account.ValidateAndInitialize();
        }
    }
}


