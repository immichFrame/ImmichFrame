using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Helpers
{
    public static class ImmichServerVersionChecker
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The minimum Immich server major version supported by this version of ImmichFrame.
        /// </summary>
        public const int MinimumSupportedMajorVersion = 3;

        /// <summary>
        /// Checks and logs the version of every configured Immich server.
        /// </summary>
        /// <returns>
        /// <c>true</c> only if every configured Immich server was reachable and reported a version of
        /// v<see cref="MinimumSupportedMajorVersion"/> or newer. Returns <c>false</c> if the configuration
        /// could not be loaded, if a server reported an older version, or if a server's version could not
        /// be determined (e.g. unreachable) — in every one of these cases ImmichFrame must not start.
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

            var allCompatible = true;

            foreach (var account in accounts)
            {
                try
                {
                    var httpClient = httpClientFactory.CreateClient("ImmichApiAccountClient");
                    httpClient.UseApiKey(account.ApiKey);
                    var immichApi = new ImmichApi(account.ImmichServerUrl, httpClient);

                    using var cts = new CancellationTokenSource(RequestTimeout);
                    var version = await immichApi.GetServerVersionAsync(cts.Token);

                    logger.LogInformation("Immich server {Url} is running v{Major}.{Minor}.{Patch}",
                        account.ImmichServerUrl, version.Major, version.Minor, version.Patch);

                    if (version.Major < MinimumSupportedMajorVersion)
                    {
                        allCompatible = false;
                        logger.LogCritical("Immich server {Url} is running v{Major}.{Minor}.{Patch}, but this version of ImmichFrame requires Immich v{Minimum} or newer. Please update your Immich server.",
                            account.ImmichServerUrl, version.Major, version.Minor, version.Patch, MinimumSupportedMajorVersion);
                    }
                }
                catch (Exception ex)
                {
                    allCompatible = false;
                    logger.LogCritical("Could not determine Immich server version for {Url}: {Message}",
                        account.ImmichServerUrl, ex.Message);
                }
            }

            return allCompatible;
        }
    }
}
