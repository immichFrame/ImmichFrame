using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Helpers
{
    public static class ImmichServerVersionLogger
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        public static async Task LogServerVersions(IServiceProvider services, ILogger logger)
        {
            IEnumerable<IAccountSettings> accounts;
            try
            {
                accounts = services.GetRequiredService<IServerSettings>().Accounts;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not check Immich server versions, config could not be loaded: {Message}", ex.Message);
                return;
            }

            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

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

                    if (version.Major < 3)
                    {
                        logger.LogWarning("Immich server {Url} is running v{Major}.{Minor}.{Patch}, but this version of ImmichFrame requires Immich v3 or newer. Please update your Immich server.",
                            account.ImmichServerUrl, version.Major, version.Minor, version.Patch);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Could not determine Immich server version for {Url}: {Message}",
                        account.ImmichServerUrl, ex.Message);
                }
            }
        }
    }
}
