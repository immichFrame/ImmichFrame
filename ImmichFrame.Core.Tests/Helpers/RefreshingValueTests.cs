using ImmichFrame.Core.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class RefreshingValueTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async Task<T> WaitFor<T>(Func<Task<T>> probe, Func<T, bool> done)
    {
        for (var i = 0; i < 200; i++)
        {
            var value = await probe();
            if (done(value))
            {
                return value;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException();
    }

    [Test]
    public async Task GetAsync_ConcurrentFirstCalls_ShareOneLoad()
    {
        var loads = 0;
        var release = new TaskCompletionSource();
        using var value = new RefreshingValue<int>(async _ =>
        {
            var n = Interlocked.Increment(ref loads);
            await release.Task;
            return n;
        }, TimeSpan.FromMinutes(5));

        var calls = Enumerable.Range(0, 10).Select(_ => Task.Run(() => value.GetAsync())).ToList();
        await Task.Delay(50);
        release.SetResult();

        Assert.That(await Task.WhenAll(calls), Is.All.EqualTo(1));
        Assert.That(loads, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAsync_WithinLifetime_DoesNotReload()
    {
        var time = new FakeTimeProvider();
        var loads = 0;
        using var value = new RefreshingValue<int>(_ => Task.FromResult(Interlocked.Increment(ref loads)), TimeSpan.FromMinutes(5), time);

        Assert.That(await value.GetAsync(), Is.EqualTo(1));
        time.Now += TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1);
        Assert.That(await value.GetAsync(), Is.EqualTo(1));
        Assert.That(loads, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAsync_Expired_ServesOldValueWhileOneRefreshRuns()
    {
        var time = new FakeTimeProvider();
        var loads = 0;
        var release = new TaskCompletionSource();
        using var value = new RefreshingValue<int>(async _ =>
        {
            var n = Interlocked.Increment(ref loads);
            if (n > 1)
            {
                await release.Task;
            }

            return n;
        }, TimeSpan.FromMinutes(5), time);

        Assert.That(await value.GetAsync(), Is.EqualTo(1));
        time.Now += TimeSpan.FromMinutes(5);

        // Many callers while the refresh is held: all get the old value at once, one refresh runs.
        for (var i = 0; i < 10; i++)
        {
            Assert.That(await value.GetAsync(), Is.EqualTo(1));
        }

        await Task.Delay(20);
        Assert.That(loads, Is.EqualTo(2));

        release.SetResult();
        Assert.That(await WaitFor(() => value.GetAsync(), v => v == 2), Is.EqualTo(2));
        Assert.That(loads, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAsync_CallerCancelled_DoesNotCancelSharedLoad()
    {
        var release = new TaskCompletionSource();
        CancellationToken loadToken = default;
        using var value = new RefreshingValue<int>(async ct =>
        {
            loadToken = ct;
            await release.Task;
            return 42;
        }, TimeSpan.FromMinutes(5));

        using var cts = new CancellationTokenSource();
        var cancelled = value.GetAsync(cts.Token);
        var other = value.GetAsync();
        cts.Cancel();

        Assert.That(async () => await cancelled, Throws.InstanceOf<OperationCanceledException>());
        release.SetResult();
        Assert.That(await other, Is.EqualTo(42));
        Assert.That(loadToken.IsCancellationRequested, Is.False);
    }

    [Test]
    public async Task GetAsync_FirstLoadFails_RethrowsUntilBackoffEndsThenLoadsAgain()
    {
        var time = new FakeTimeProvider();
        var loads = 0;
        using var value = new RefreshingValue<int>(_ =>
        {
            var n = Interlocked.Increment(ref loads);
            return n == 1 ? throw new HttpRequestException("down") : Task.FromResult(n);
        }, TimeSpan.FromMinutes(5), time);

        Assert.That(async () => await value.GetAsync(), Throws.InstanceOf<HttpRequestException>());
        time.Now += RefreshingValue<int>.InitialBackoff - TimeSpan.FromSeconds(1);
        Assert.That(async () => await value.GetAsync(), Throws.InstanceOf<HttpRequestException>());
        Assert.That(loads, Is.EqualTo(1));

        time.Now += TimeSpan.FromSeconds(1);
        Assert.That(await value.GetAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAsync_RefreshFails_KeepsServingOldValue()
    {
        var time = new FakeTimeProvider();
        var loads = 0;
        using var value = new RefreshingValue<int>(_ =>
        {
            var n = Interlocked.Increment(ref loads);
            return n == 2 ? throw new HttpRequestException("down") : Task.FromResult(n);
        }, TimeSpan.FromMinutes(5), time);

        Assert.That(await value.GetAsync(), Is.EqualTo(1));
        time.Now += TimeSpan.FromMinutes(5);
        Assert.That(await value.GetAsync(), Is.EqualTo(1));
        await WaitFor(() => Task.FromResult(Volatile.Read(ref loads)), n => n == 2);
        await Task.Delay(20);

        // The failed refresh is retried once the backoff has passed; until then the old value is served.
        time.Now += RefreshingValue<int>.InitialBackoff;
        Assert.That(await WaitFor(() => value.GetAsync(), v => v == 3), Is.EqualTo(3));
    }

    [Test]
    public async Task GetAsync_RefreshKeepsFailing_BacksOffDoublingUpToTheLifetime()
    {
        var time = new FakeTimeProvider();
        var lifetime = TimeSpan.FromMinutes(5);
        var loads = 0;
        using var value = new RefreshingValue<int>(_ =>
        {
            var n = Interlocked.Increment(ref loads);
            return n > 1 ? throw new HttpRequestException("down") : Task.FromResult(n);
        }, lifetime, time);

        await value.GetAsync();
        time.Now += lifetime;

        // 30s, 60s, 120s, 240s, then capped at the 5-minute lifetime.
        var expected = new[] { 30, 60, 120, 240, 300, 300 }.Select(s => TimeSpan.FromSeconds(s));
        var attempts = 1;
        foreach (var delay in expected)
        {
            await value.GetAsync();
            attempts++;
            await WaitFor(() => Task.FromResult(Volatile.Read(ref loads)), n => n == attempts);
            // Let the failed load record its backoff before the clock moves.
            await Task.Delay(20);

            time.Now += delay - TimeSpan.FromSeconds(1);
            for (var i = 0; i < 5; i++)
            {
                Assert.That(await value.GetAsync(), Is.EqualTo(1));
            }

            await Task.Delay(20);
            Assert.That(loads, Is.EqualTo(attempts), $"retried before the {delay} backoff ended");
            time.Now += TimeSpan.FromSeconds(1);
        }
    }

    [Test]
    public void GetAsync_NoValueAndLoadKeepsFailing_NeverWaitsLongerThanTheFloor()
    {
        var time = new FakeTimeProvider();
        var loads = 0;
        using var value = new RefreshingValue<int>(_ =>
        {
            Interlocked.Increment(ref loads);
            return Task.FromException<int>(new HttpRequestException("down"));
        }, TimeSpan.FromHours(12), time);

        for (var attempt = 1; attempt <= 12; attempt++)
        {
            Assert.That(async () => await value.GetAsync(), Throws.InstanceOf<HttpRequestException>());
            Assert.That(loads, Is.EqualTo(attempt));
            time.Now += RefreshingValue<int>.MaxBackoffWithoutValue;
        }
    }

    [Test]
    public async Task GetAsync_LongLifetimeRefreshKeepsFailing_RetriesWithinMaxBackoff()
    {
        var time = new FakeTimeProvider();
        var lifetime = TimeSpan.FromHours(12);
        var loads = 0;
        using var value = new RefreshingValue<int>(_ =>
        {
            var n = Interlocked.Increment(ref loads);
            return n > 1 ? throw new HttpRequestException("down") : Task.FromResult(n);
        }, lifetime, time);

        await value.GetAsync();
        time.Now += lifetime;

        for (var attempts = 2; attempts <= 12; attempts++)
        {
            Assert.That(await value.GetAsync(), Is.EqualTo(1));
            await WaitFor(() => Task.FromResult(Volatile.Read(ref loads)), n => n == attempts);
            // Let the failed load record its backoff before the clock moves.
            await Task.Delay(20);
            time.Now += RefreshingValue<int>.MaxBackoff;
        }
    }

    [Test]
    public async Task GetAsync_RefreshFails_LogsTheFailure()
    {
        var time = new FakeTimeProvider();
        var logger = new Mock<ILogger>();
        var loads = 0;
        var error = new HttpRequestException("down");
        using var value = new RefreshingValue<int>(_ =>
        {
            var n = Interlocked.Increment(ref loads);
            return n > 1 ? throw error : Task.FromResult(n);
        }, TimeSpan.FromMinutes(5), time, logger.Object, description: "asset ID listing");

        await value.GetAsync();
        time.Now += TimeSpan.FromMinutes(5);
        await value.GetAsync();
        await WaitFor(() => Task.FromResult(Volatile.Read(ref loads)), n => n == 2);
        await Task.Delay(20);

        logger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("asset ID listing")),
            error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task GetAsync_ServedValueOlderThanMaxAge_WarnsOnce()
    {
        var time = new FakeTimeProvider();
        var logger = new Mock<ILogger>();
        var loads = 0;
        using var value = new RefreshingValue<int>(_ =>
        {
            var n = Interlocked.Increment(ref loads);
            return n > 1 ? throw new HttpRequestException("down") : Task.FromResult(n);
        }, TimeSpan.FromMinutes(5), time, logger.Object, maxAge: TimeSpan.FromMinutes(20));

        await value.GetAsync();
        time.Now += TimeSpan.FromMinutes(20);
        await value.GetAsync();
        VerifyStaleWarnings(logger, Times.Never());

        for (var i = 0; i < 3; i++)
        {
            time.Now += TimeSpan.FromMinutes(5);
            Assert.That(await value.GetAsync(), Is.EqualTo(1));
        }

        VerifyStaleWarnings(logger, Times.Once());
    }

    private static void VerifyStaleWarnings(Mock<ILogger> logger, Times times) =>
        logger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Still serving")),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
}
