using System.Diagnostics.Contracts;

namespace ImmichFrame.Core.Logic.QueueMutator;

public interface IQueueMutator<T>
{
    [Pure]
    IEnumerable<T> Mutate(IEnumerable<T> source);
    void SetNext(IQueueMutator<T> next);
}