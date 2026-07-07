namespace ImmichFrame.Core.Logic.QueueMutator;

public class RandomQueueMutator<T>: QueueMutatorBase<T>
{
    private readonly Random _random = new();
    public override IEnumerable<T> Mutate(IEnumerable<T> source)
    {
        var result = source.OrderBy(_ => _random.Next());
        return Next?.Mutate(result) ?? result;
    }
}