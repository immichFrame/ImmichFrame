using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.QueueMutator;

public class DuplicateAvoidingQueueMutator: QueueMutatorBase<AssetResponseDto>
{
    private readonly int _batchSize;
    private readonly ILogger<DuplicateAvoidingQueueMutator> _logger;

    public DuplicateAvoidingQueueMutator(int batchSize, ILogger<DuplicateAvoidingQueueMutator> logger)
    {
        if(batchSize <= 0) throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));
        _batchSize = batchSize;
        _logger = logger;
    }
    public override IEnumerable<AssetResponseDto> Mutate(IEnumerable<AssetResponseDto> source)
    {
        var remaining = new Queue<AssetResponseDto>(source);
        List<AssetResponseDto> result = new(remaining.Count);
        var batch = new List<AssetResponseDto>(_batchSize);

        int consecutiveDuplicates = 0;
        while (remaining.Count > 0)
        {
            var currentItem = remaining.Dequeue();

            bool landscape = false;
            try
            {

                landscape = ImageHelper.IsLandscape(currentItem.ExifInfo.ExifImageWidth!.Value,
                    currentItem.ExifInfo.ExifImageHeight!.Value, currentItem.ExifInfo.Orientation);
            }
            catch
            {
                _logger.LogWarning("Failed to determine orientation for asset {AssetId}, defaulting to portrait", currentItem.Id);
            }

            
            if (!landscape && batch.Any(x => x.Id == currentItem.Id))
            {
                remaining.Enqueue(currentItem);
                consecutiveDuplicates++;
                // no other items to process, break to avoid infinite loop
                if (consecutiveDuplicates >= remaining.Count) break;
                continue;
            }

            batch.Add(currentItem);

            if (batch.Count >= _batchSize)
            {
                result.AddRange(batch);
                batch.Clear();
            }

            consecutiveDuplicates = 0;
        }

        if (batch.Count > 0)
        {
            result.AddRange(batch);
        }

        return Next?.Mutate(result) ?? result;
    }
}