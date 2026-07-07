using ImmichFrame.Core.Api;
using ImmichFrame.Core.Logic.QueueMutator;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic.QueueMutator;

[TestFixture]
public class DuplicateAvoidingQueueMutatorTests
{
    private IQueueMutator<AssetResponseDto> _queueMutator;

    [SetUp]
    public void Setup()
    {
        _queueMutator = new DuplicateAvoidingQueueMutator(2, new NullLogger<DuplicateAvoidingQueueMutator>());
    }
    
    [Test]
    public void Mutate_NoItems_Succeeds()
    {
        IEnumerable<AssetResponseDto> result = [new()];
        
        Assert.DoesNotThrow(() => result = _queueMutator.Mutate([]));
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void Mutate_NoDuplicates_NoChange()
    {
        List<AssetResponseDto> original = [
            new(){Id = "aap"},
            new(){Id = "noot"},
            new(){Id = "mies"},
            new(){Id = "aardappel"},
        ];
        IEnumerable<AssetResponseDto> result = [];
        IEnumerable<AssetResponseDto> input = new List<AssetResponseDto>(original);
        
        Assert.DoesNotThrow(() => result = _queueMutator.Mutate(input));
        Assert.That(result, Is.EqualTo(original));
    }
    
    [Test]
    public void Mutate_SeparatedDuplicates_NoChange()
    {
        List<AssetResponseDto> original = [
            new(){Id = "aap"},
            new(){Id = "mies"},
            new(){Id = "noot"},
            new(){Id = "mies"},
            new(){Id = "aardappel"},
            new(){Id = "mies"},
        ];
        IEnumerable<AssetResponseDto> result = [];
        IEnumerable<AssetResponseDto> input = new List<AssetResponseDto>(original);
        
        Assert.DoesNotThrow(() => result = _queueMutator.Mutate(input));
        Assert.That(result, Is.EqualTo(original));
    }
    
    [Test]
    public void Mutate_AdjacentDuplicates_ShouldSeparate()
    {
        List<AssetResponseDto> original = [
            new(){Id = "aap"},
            new(){Id = "aap"},
            new(){Id = "noot"},
            new(){Id = "mies"},
            new(){Id = "aardappel"},
        ];
        IEnumerable<AssetResponseDto> result = [];
        IEnumerable<AssetResponseDto> input = new List<AssetResponseDto>(original);
        
        Assert.DoesNotThrow(() => result = _queueMutator.Mutate(input));
        var assetResponseDtos = result as AssetResponseDto[] ?? result.ToArray();
        for(int i = 0; i < assetResponseDtos.Length - 1; i++)
        {
            Assert.That(assetResponseDtos.ElementAt(i).Id, Is.Not.EqualTo(assetResponseDtos.ElementAt(i + 1).Id));
        }
    }
    [Test]
    public void Mutate_AdjacentDuplicates_differentBatch_NoChange()
    {
        List<AssetResponseDto> original = [
            new(){Id = "aap"},
            new(){Id = "noot"},
            new(){Id = "noot"},
            new(){Id = "mies"},
            new(){Id = "aardappel"},
        ];
        IEnumerable<AssetResponseDto> result = [];
        IEnumerable<AssetResponseDto> input = new List<AssetResponseDto>(original);
        
        Assert.DoesNotThrow(() => result = _queueMutator.Mutate(input));
        Assert.That(result, Is.EqualTo(original));
    }
    [Test]
    public void Mutate_LandscapeDuplicates_adjacent_NoChange()
    {
        List<AssetResponseDto> original = [
            new(){Id = "aap"},
            new(){Id = "peter"},
            new(){Id = "noot", ExifInfo =new(){Orientation = "1", ExifImageWidth = 200, ExifImageHeight = 100}},
            new(){Id = "noot", ExifInfo =new(){Orientation = "1", ExifImageWidth = 200, ExifImageHeight = 100}},
            new(){Id = "mies"},
            new(){Id = "aardappel"},
        ];
        IEnumerable<AssetResponseDto> result = [];
        IEnumerable<AssetResponseDto> input = new List<AssetResponseDto>(original);
        
        Assert.DoesNotThrow(() => result = _queueMutator.Mutate(input));
        Assert.That(result, Is.EqualTo(original));
    }
}