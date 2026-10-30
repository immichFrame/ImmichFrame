using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Logic.Pool;

/// <summary>
/// Deals a source's assets from a shuffled deck, so every asset is shown once before any repeats.
/// Each client gets its own deck; callers without a client ID, and clients turned away by the
/// limits below, share one.
/// </summary>
public class ShuffleDeckPool(IDealableAssetPool source, TimeProvider? timeProvider = null) : IAssetPool
{
    // The client ID comes from the caller, so the number of decks, and how fast new ones appear, are bounded.
    public const int MaxDecks = 32;
    public const int MaxClientIdLength = 64;
    public static readonly TimeSpan IdleExpiry = TimeSpan.FromHours(24);

    /// <summary>Past this many client decks, at most one new deck is made per <see cref="NewDeckInterval"/>.</summary>
    public const int RateLimitedAbove = 8;
    public static readonly TimeSpan NewDeckInterval = TimeSpan.FromMinutes(1);

    /// <summary>A deck used this recently is never evicted to make room for a new one.</summary>
    public static readonly TimeSpan ProtectedFor = TimeSpan.FromMinutes(10);

    /// <summary>
    /// A call deals at most this many cards per asset requested, counting the ones that could not
    /// be loaded (deleted, trashed or filtered out) and are replaced. Each card costs the source a load.
    /// A round with a passing failure ends the call, so an outage costs one round per call.
    /// </summary>
    public const int LoadBudgetPerAsset = 2;

    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    // Guards only the registry below. Each deck is locked on its own entry, so syncing or
    // shuffling a large deck never holds up other clients.
    private readonly object _registryGate = new();
    private readonly Dictionary<string, DeckEntry> _decks = new(StringComparer.Ordinal);
    private readonly DeckEntry _shared = new(new ShuffleDeck(new Random()), DateTimeOffset.MinValue);
    private DateTimeOffset _lastDeckCreated = DateTimeOffset.MinValue;

    private sealed class DeckEntry(ShuffleDeck deck, DateTimeOffset lastUsed)
    {
        public ShuffleDeck Deck { get; } = deck;
        public DateTimeOffset LastUsed { get; set; } = lastUsed;
    }

    public Task<long> GetAssetCount(CancellationToken ct = default) => source.GetAssetCount(ct);

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, string clientId = "", CancellationToken ct = default)
    {
        if (requested <= 0)
        {
            return [];
        }

        var list = await source.GetAllAssetIds(ct);
        var entry = GetDeck(NormalizeClientId(clientId));

        var assets = new List<AssetResponseDto>(requested);
        var seen = new HashSet<Guid>();
        // Dealt but not shown: they go back to the deck when this call ends.
        var unshown = new List<Guid>();
        IReadOnlyList<Guid> pending = [];
        var budget = requested > int.MaxValue / LoadBudgetPerAsset ? int.MaxValue : requested * LoadBudgetPerAsset;
        try
        {
            while (assets.Count < requested && budget > 0)
            {
                lock (entry)
                {
                    entry.Deck.Sync(list);
                    var dealt = entry.Deck.Deal(Math.Min(requested - assets.Count, budget));
                    budget -= dealt.Count;
                    // A card dealt again after the deck wrapped within this call counts for the next cycle.
                    pending = dealt.Where(seen.Add).ToList();
                    unshown.AddRange(dealt.Except(pending));
                }

                if (pending.Count == 0)
                {
                    break;
                }

                var result = await source.LoadAssetsByIds(pending, ct);
                pending = [];
                assets.AddRange(result.Loaded);
                unshown.AddRange(result.Retry);

                // A passing failure means Immich is struggling; dealing more would only add to its load.
                if (result.Retry.Count > 0)
                {
                    break;
                }
            }
        }
        finally
        {
            unshown.AddRange(pending);
            if (unshown.Count > 0)
            {
                lock (entry)
                {
                    entry.Deck.Return(unshown);
                }
            }
        }

        return assets;
    }

    /// <summary>The number of client decks, not counting the shared one.</summary>
    internal int DeckCount
    {
        get
        {
            lock (_registryGate)
            {
                return _decks.Count;
            }
        }
    }

    /// <summary>The listing a client's own deck deals from, or null when it has none.</summary>
    internal AssetIdList? ListingOf(string clientId)
    {
        lock (_registryGate)
        {
            return _decks.TryGetValue(NormalizeClientId(clientId), out var entry) ? entry.Deck.Listing : null;
        }
    }

    internal static string NormalizeClientId(string? clientId)
    {
        var key = clientId?.Trim() ?? string.Empty;
        return key.Length > MaxClientIdLength ? key[..MaxClientIdLength] : key;
    }

    // Finds or makes the deck for this key. Never does work proportional to the deck size.
    private DeckEntry GetDeck(string key)
    {
        lock (_registryGate)
        {
            var now = _time.GetUtcNow();

            foreach (var expired in _decks.Where(d => now - d.Value.LastUsed > IdleExpiry).Select(d => d.Key).ToList())
            {
                _decks.Remove(expired);
            }

            if (key.Length > 0 && _decks.TryGetValue(key, out var entry))
            {
                entry.LastUsed = now;
                return entry;
            }

            if (key.Length == 0 || !MayCreateDeck(now))
            {
                _shared.LastUsed = now;
                return _shared;
            }

            entry = new DeckEntry(new ShuffleDeck(new Random()), now);
            _decks[key] = entry;
            _lastDeckCreated = now;
            return entry;
        }
    }

    // Caller holds _registryGate. Evicts the least recently used deck when that makes room.
    private bool MayCreateDeck(DateTimeOffset now)
    {
        if (_decks.Count >= RateLimitedAbove && now - _lastDeckCreated < NewDeckInterval)
        {
            return false;
        }

        if (_decks.Count < MaxDecks)
        {
            return true;
        }

        var leastRecent = _decks.MinBy(d => d.Value.LastUsed);
        if (now - leastRecent.Value.LastUsed < ProtectedFor)
        {
            return false;
        }

        _decks.Remove(leastRecent.Key);
        return true;
    }
}
