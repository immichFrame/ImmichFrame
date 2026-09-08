using System.Security.Cryptography;
using System.Text;

namespace ImmichFrame.WebApi.Services;

/// <summary>What the admin UI should show.</summary>
public enum AdminUiState
{
    /// <summary>Fresh install with no password anywhere — anonymous onboarding is open.</summary>
    Setup,

    /// <summary>An admin password exists; ask for it.</summary>
    Login,

    /// <summary>
    /// Configured, but no admin password: the instance is already somebody's, so
    /// onboarding stays shut. Only the environment variable gets you back in.
    /// </summary>
    Disabled
}

/// <summary>
/// Resolves the admin password. The environment variable always wins so a lockout
/// (wrong password saved via the UI) can be recovered without touching the database.
/// If neither source is set, the admin interface is disabled.
/// </summary>
public class AdminAuthService(SettingsService _settingsService)
{
    public const string AdminPasswordEnvVar = "IMMICHFRAME_ADMIN_PASSWORD";

    public string? GetAdminPassword()
    {
        var fromEnv = Environment.GetEnvironmentVariable(AdminPasswordEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var fromSettings = _settingsService.Current.GeneralSettings.AdminPassword;
        return string.IsNullOrWhiteSpace(fromSettings) ? null : fromSettings;
    }

    public bool AdminEnabled => GetAdminPassword() != null;

    /// <summary>
    /// Onboarding only opens on a genuinely fresh install. An instance that already has
    /// settings — imported from a config file or saved earlier — is somebody's, even
    /// without an admin password, so it reports <see cref="AdminUiState.Disabled"/> instead.
    /// </summary>
    public bool SetupRequired => GetAdminPassword() == null && _settingsService.IsUnconfigured;

    public AdminUiState State => AdminEnabled
        ? AdminUiState.Login
        : SetupRequired ? AdminUiState.Setup : AdminUiState.Disabled;

    public bool ValidatePassword(string candidate)
    {
        var password = GetAdminPassword();
        if (password == null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(candidate),
            Encoding.UTF8.GetBytes(password));
    }
}
