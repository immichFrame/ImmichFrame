using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Helpers
{
    public record AccountCheckResult(bool Success, string Message, string? Version);

    public static class ImmichServerVersionChecker
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The minimum Immich server version supported by this version of ImmichFrame.
        /// </summary>
        public const int MinimumSupportedMajorVersion = 3;
        public const int MinimumSupportedMinorVersion = 2;
        public const int MinimumSupportedPatchVersion = 0;

        public static readonly Version MinimumSupportedVersion = new(
            MinimumSupportedMajorVersion,
            MinimumSupportedMinorVersion,
            MinimumSupportedPatchVersion);

        /// <summary>
        /// Checks a single account: is the Immich server reachable and running a supported version?
        /// </summary>
        public static async Task<AccountCheckResult> CheckAccount(IAccountSettings account, IHttpClientFactory httpClientFactory)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ImmichApiAccountClient");
                httpClient.UseApiKey(account.ApiKey);
                var immichApi = new ImmichApi(account.ImmichServerUrl, httpClient);

                using var cts = new CancellationTokenSource(RequestTimeout);
                var version = await immichApi.GetServerVersionAsync(cts.Token);
                var versionString = $"{version.Major}.{version.Minor}.{version.Patch}";

                if (!IsSupported(version))
                {
                    return new AccountCheckResult(false,
                        $"Immich server {account.ImmichServerUrl} is running v{versionString}, but this version of ImmichFrame requires Immich v{MinimumSupportedVersion} or newer. Please update your Immich server.",
                        versionString);
                }

                return new AccountCheckResult(true,
                    $"Immich server {account.ImmichServerUrl} is running v{versionString}", versionString);
            }
            catch (Exception ex)
            {
                return new AccountCheckResult(false,
                    $"Could not determine Immich server version for {account.ImmichServerUrl}: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Checks and logs the version of every configured Immich server.
        /// </summary>
        /// <returns>
        /// <c>true</c> only if every configured Immich server was reachable and reported a version of
        /// v<see cref="MinimumSupportedVersion"/> or newer.
        /// </returns>
        public static async Task<bool> CheckServerVersions(IServiceProvider services, ILogger logger)
        {
            IEnumerable<IAccountSettings> accounts;
            try
            {
                accounts = services.GetRequiredService<IServerSettings>().Accounts;
            }
            catch (Exception ex)
            {
                logger.LogCritical("Could not check Immich server versions, config could not be loaded: {Message}", ex.Message);
                return false;
            }

            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

            // In parallel: one unreachable server must not add its timeout to all the others
            var results = await Task.WhenAll(accounts.Select(account => CheckAccount(account, httpClientFactory)));

            foreach (var result in results)
            {
                if (result.Success)
                {
                    logger.LogInformation("{Message}", result.Message);
                }
                else
                {
                    logger.LogCritical("{Message}", result.Message);
                }
            }

            return results.All(r => r.Success);
        }

        private static bool IsSupported(ServerVersionResponseDto version)
        {
            var numeric = new Version((int)version.Major, (int)version.Minor, (int)version.Patch);
            if (numeric == MinimumSupportedVersion && version.Prerelease != null)
                return false;

            return numeric >= MinimumSupportedVersion;
        }
    }
}
