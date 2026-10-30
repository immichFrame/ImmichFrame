namespace ImmichFrame.Core.Logic.Pool;

/// <summary>
/// A shuffled deck of asset IDs: every member is dealt once per cycle before any is dealt again.
/// Members can be added or removed mid-cycle without restarting it. The member set belongs to the
/// shared <see cref="AssetIdList"/>; the deck keeps only its own undealt order. Not thread-safe;
/// the owner locks.
/// </summary>
public sealed class ShuffleDeck(Random random)
{
    /// <summary>How many of the most recently dealt cards are kept out of the start of the next cycle.</summary>
    public const int RecentWindow = 25;

    private static readonly AssetIdList NoMembers = new([]);

    private AssetIdList _list = NoMembers;
    private bool _synced;

    // The rest of the current cycle, dealt from the end, and the same cards as a set.
    private readonly List<Guid> _undealt = [];
    private readonly HashSet<Guid> _undealtSet = [];

    // The last cards dealt, oldest first.
    private readonly List<Guid> _recent = [];
    private int _recentCapacity = RecentWindow;

    // The cards the last refill kept out of the start of its cycle, until each is dealt.
    private readonly HashSet<Guid> _held = [];

    /// <summary>The version of the listing this deck was last synced with, or -1 before the first sync.</summary>
    public long Version => _synced ? _list.Version : -1;

    public int Count => _list.Count;

    /// <summary>The listing this deck deals from, shared with every other deck synced to it.</summary>
    internal AssetIdList Listing => _list;

    /// <summary>Syncs with a fresh listing of these IDs.</summary>
    public void Sync(IReadOnlyCollection<Guid> ids) => Sync(new AssetIdList(ids));

    /// <summary>
    /// Moves the deck to this listing. A listing older than (or the same as) the one already
    /// synced is ignored, so a slow request can never take the deck back. Returns whether it synced.
    /// </summary>
    public bool Sync(AssetIdList list)
    {
        if (_synced && list.Version <= _list.Version)
        {
            return false;
        }

        var previous = _list;
        var members = list.Members;
        _undealt.RemoveAll(id => !members.Contains(id));
        _undealtSet.RemoveWhere(id => !members.Contains(id));
        _recent.RemoveAll(id => !members.Contains(id));
        _held.RemoveWhere(id => !members.Contains(id));

        // Newcomers join the rest of the current cycle at a uniformly random position. With nothing
        // left in the cycle (including the first sync) they wait for the next one, which shuffles
        // every member.
        if (_undealt.Count > 0)
        {
            foreach (var id in list.Ids)
            {
                if (!previous.Members.Contains(id))
                {
                    AddUndealt(id);
                }
            }
        }

        _list = list;
        _synced = true;
        return true;
    }

    public IReadOnlyList<Guid> Deal(int n)
    {
        if (n <= 0 || _list.Count == 0)
        {
            return [];
        }

        // A batch never holds the same card twice.
        n = Math.Min(n, _list.Count);
        _recentCapacity = Math.Max(_recentCapacity, n);

        var dealt = new List<Guid>(n);
        while (dealt.Count < n)
        {
            if (_undealt.Count == 0)
            {
                Refill(need: n - dealt.Count, inBatch: dealt.Count);
            }

            var card = _undealt[^1];
            _undealt.RemoveAt(_undealt.Count - 1);
            _undealtSet.Remove(card);
            _held.Remove(card);
            dealt.Add(card);
            Remember(card);
        }

        return dealt;
    }

    /// <summary>
    /// Puts dealt cards that were never shown back into the rest of the cycle, at random positions,
    /// so they are dealt again later. Cards no longer in the listing are dropped.
    /// </summary>
    public void Return(IEnumerable<Guid> cards)
    {
        foreach (var card in cards)
        {
            if (_list.Members.Contains(card) && !_undealtSet.Contains(card))
            {
                AddUndealt(card);
            }
        }
    }

    // Puts a card at a uniformly random place in the rest of the cycle without shifting the list:
    // it takes the place of the card there, which moves to the end and is dealt next. A recently
    // card held back by the last refill must stay away from the end, so in that case the card is inserted instead.
    private void AddUndealt(Guid card)
    {
        _undealtSet.Add(card);
        var position = random.Next(_undealt.Count + 1);
        if (position == _undealt.Count)
        {
            _undealt.Add(card);
            return;
        }

        var displaced = _undealt[position];
        if (_held.Contains(displaced))
        {
            _undealt.Insert(position, card);
            return;
        }

        _undealt[position] = card;
        _undealt.Add(displaced);
    }

    private void Remember(Guid card)
    {
        _recent.Add(card);
        if (_recent.Count > _recentCapacity)
        {
            _recent.RemoveRange(0, _recent.Count - _recentCapacity);
        }
    }

    // Starts a new cycle. The most recently dealt cards are kept out of its start, so a batch that
    // spans the join never repeats a card and the end of one cycle doesn't reappear at the start of the next.
    private void Refill(int need, int inBatch)
    {
        var members = _list.Members;
        var n = _list.Count;
        var avoidCount = Math.Min(Math.Max(inBatch, Math.Min(RecentWindow, n / 2)), n - need);

        var avoid = new List<Guid>(avoidCount);
        var avoidSet = new HashSet<Guid>();
        for (var i = _recent.Count - 1; i >= 0 && avoid.Count < avoidCount; i--)
        {
            var card = _recent[i];
            if (members.Contains(card) && avoidSet.Add(card))
            {
                avoid.Add(card);
            }
        }

        var order = _list.Ids.Where(id => !avoidSet.Contains(id)).ToList();
        Shuffle(order);
        var guard = Math.Min(order.Count, Math.Max(RecentWindow, need));

        Shuffle(avoid);
        foreach (var card in avoid)
        {
            order.Insert(random.Next(guard, order.Count + 1), card);
        }

        if (n >= 2 && _recent.Count > 0 && order[0] == _recent[^1])
        {
            var other = random.Next(1, order.Count);
            (order[0], order[other]) = (order[other], order[0]);
        }

        _held.Clear();
        _held.UnionWith(avoid);
        if (n >= 2 && _recent.Count > 0)
        {
            _held.Add(_recent[^1]);
        }

        order.Reverse();
        _undealt.AddRange(order);
        _undealtSet.UnionWith(order);
    }

    private void Shuffle(List<Guid> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
