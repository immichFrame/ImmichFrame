namespace ImmichFrame.Core.Logic.QueueMutator;

public abstract class QueueMutatorBase<T>: IQueueMutator<T>
{
    protected IQueueMutator<T>? Next;

    public abstract IEnumerable<T> Mutate(IEnumerable<T> source);

    public void SetNext(IQueueMutator<T> next)
    {
        Next = next;
    }
}