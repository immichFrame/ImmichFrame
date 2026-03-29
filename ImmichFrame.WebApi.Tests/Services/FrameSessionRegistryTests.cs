using ImmichFrame.Core.Api;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Services;

[TestFixture]
public class FrameSessionRegistryTests
{
    [Test]
    public void UpsertSnapshot_TracksActiveSessionAndHistory()
    {
        var now = new DateTimeOffset(2026, 03, 25, 12, 00, 00, TimeSpan.Zero);
        var registry = new FrameSessionRegistry(new FrameSessionRegistryOptions(), () => now);

        registry.UpsertSnapshot("frame-kitchen", new FrameSessionSnapshotDto
        {
            PlaybackState = FramePlaybackState.Playing,
            Status = FrameSessionStatus.Active,
            CurrentDisplay = new DisplayEventDto
            {
                DisplayedAtUtc = now,
                DurationSeconds = 45,
                Assets =
                [
                    new DisplayedAssetDto
                    {
                        Id = "asset-1",
                        OriginalFileName = "family.jpg",
                        Type = AssetTypeEnum.IMAGE,
                        ImmichServerUrl = "http://photos.example",
                        LocalDateTime = now.ToString("O")
                    }
                ]
            },
            History =
            [
                new DisplayEventDto
                {
                    DisplayedAtUtc = now.AddMinutes(-2),
                    DurationSeconds = 30,
                    Assets =
                    [
                        new DisplayedAssetDto
                        {
                            Id = "asset-0",
                            OriginalFileName = "vacation.jpg",
                            Type = AssetTypeEnum.IMAGE,
                            LocalDateTime = now.AddDays(-1).ToString("O")
                        }
                    ]
                }
            ]
        }, "NUnit-Agent");

        var sessions = registry.GetActiveSessions();

        Assert.That(sessions, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(sessions[0].ClientIdentifier, Is.EqualTo("frame-kitchen"));
            Assert.That(sessions[0].PlaybackState, Is.EqualTo(FramePlaybackState.Playing));
            Assert.That(sessions[0].Status, Is.EqualTo(FrameSessionStatus.Active));
            Assert.That(sessions[0].CurrentDisplay?.Assets[0].OriginalFileName, Is.EqualTo("family.jpg"));
            Assert.That(sessions[0].CurrentDisplay?.Assets[0].ImmichServerUrl, Is.EqualTo("http://photos.example"));
            Assert.That(sessions[0].CurrentDisplay?.DurationSeconds, Is.EqualTo(45));
            Assert.That(sessions[0].History, Has.Count.EqualTo(1));
            Assert.That(sessions[0].History[0].DurationSeconds, Is.EqualTo(30));
            Assert.That(sessions[0].UserAgent, Is.EqualTo("NUnit-Agent"));
        });
    }

    [Test]
    public void CommandQueue_CanBeAcknowledged()
    {
        var now = new DateTimeOffset(2026, 03, 25, 12, 00, 00, TimeSpan.Zero);
        var registry = new FrameSessionRegistry(new FrameSessionRegistryOptions(), () => now);

        registry.UpsertSnapshot("frame-office", new FrameSessionSnapshotDto(), "NUnit-Agent");

        var result = registry.EnqueueCommand("frame-office", FrameAdminCommandType.Next);
        var pending = registry.GetPendingCommands("frame-office");

        Assert.That(result.Status, Is.EqualTo(FrameSessionCommandEnqueueStatus.Enqueued));
        Assert.That(result.Command, Is.Not.Null);
        Assert.That(pending, Has.Count.EqualTo(1));
        Assert.That(registry.AcknowledgeCommand("frame-office", result.Command!.CommandId), Is.True);
        Assert.That(registry.GetPendingCommands("frame-office"), Is.Empty);
    }

    [Test]
    public void StaleSessions_ArePruned()
    {
        var now = new DateTimeOffset(2026, 03, 25, 12, 00, 00, TimeSpan.Zero);
        var registry = new FrameSessionRegistry(new FrameSessionRegistryOptions
        {
            HeartbeatTtl = TimeSpan.FromSeconds(30),
            StaleSessionTtl = TimeSpan.FromMinutes(5)
        }, () => now);

        registry.UpsertSnapshot("frame-den", new FrameSessionSnapshotDto(), "NUnit-Agent");

        now = now.AddMinutes(6);

        Assert.That(registry.GetActiveSessions(), Is.Empty);
        Assert.That(
            registry.EnqueueCommand("frame-den", FrameAdminCommandType.Refresh).Status,
            Is.EqualTo(FrameSessionCommandEnqueueStatus.NotFound));
    }

    [Test]
    public void DisplayName_PersistsAcrossRegistryInstances()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-frame-names.json");
        try
        {
            var now = new DateTimeOffset(2026, 03, 25, 12, 00, 00, TimeSpan.Zero);
            var options = new FrameSessionRegistryOptions
            {
                DisplayNameStorePath = tempFile
            };

            var writerRegistry = new FrameSessionRegistry(options, () => now);
            writerRegistry.UpsertSnapshot("frame-kitchen", new FrameSessionSnapshotDto(), "NUnit-Agent");
            Assert.That(writerRegistry.UpdateDisplayName("frame-kitchen", "Kitchen Frame"), Is.True);

            var readerRegistry = new FrameSessionRegistry(options, () => now);
            readerRegistry.UpsertSnapshot("frame-kitchen", new FrameSessionSnapshotDto(), "NUnit-Agent");

            var sessions = readerRegistry.GetActiveSessions();
            Assert.That(sessions, Has.Count.EqualTo(1));
            Assert.That(sessions[0].DisplayName, Is.EqualTo("Kitchen Frame"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Test]
    public void UpsertSnapshot_WithNullDisplayEventAssets_ClonesAsEmptyList()
    {
        var now = new DateTimeOffset(2026, 03, 25, 12, 00, 00, TimeSpan.Zero);
        var registry = new FrameSessionRegistry(new FrameSessionRegistryOptions(), () => now);

        registry.UpsertSnapshot("frame-porch", new FrameSessionSnapshotDto
        {
            CurrentDisplay = new DisplayEventDto
            {
                DisplayedAtUtc = now,
                Assets = null!
            },
            History =
            [
                new DisplayEventDto
                {
                    DisplayedAtUtc = now.AddMinutes(-1),
                    Assets = null!
                }
            ]
        }, "NUnit-Agent");

        var session = registry.GetActiveSessions().Single();

        Assert.That(session.CurrentDisplay, Is.Not.Null);
        Assert.That(session.CurrentDisplay!.Assets, Is.Not.Null);
        Assert.That(session.CurrentDisplay.Assets, Is.Empty);
        Assert.That(session.History, Has.Count.EqualTo(1));
        Assert.That(session.History[0].Assets, Is.Not.Null);
        Assert.That(session.History[0].Assets, Is.Empty);
    }

    [Test]
    public void UpsertSnapshot_WithNullHistory_StoresEmptyHistory()
    {
        var now = new DateTimeOffset(2026, 03, 25, 12, 00, 00, TimeSpan.Zero);
        var registry = new FrameSessionRegistry(new FrameSessionRegistryOptions(), () => now);

        registry.UpsertSnapshot("frame-patio", new FrameSessionSnapshotDto
        {
            CurrentDisplay = new DisplayEventDto
            {
                DisplayedAtUtc = now,
                Assets =
                [
                    new DisplayedAssetDto
                    {
                        Id = "asset-7",
                        OriginalFileName = "garden.jpg",
                        Type = AssetTypeEnum.IMAGE
                    }
                ]
            },
            History = null!
        }, "NUnit-Agent");

        var session = registry.GetActiveSessions().Single();

        Assert.That(session.CurrentDisplay, Is.Not.Null);
        Assert.That(session.History, Is.Not.Null);
        Assert.That(session.History, Is.Empty);
    }
}
