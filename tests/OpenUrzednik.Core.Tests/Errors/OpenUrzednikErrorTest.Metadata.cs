using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class OpenUrzednikErrorTest
{
    [Fact]
    public void Metadata_NotRead_DoesNotCollect()
    {
        // Act
        var error = new CollectingError();

        // Assert
        error.CollectCount.ShouldBe(0);
    }

    [Fact]
    public void Metadata_ReadTwice_CollectsOnceAndReturnsTheSameInstance()
    {
        // Arrange
        var error = new CollectingError();

        // Act
        var first = error.Metadata;
        var second = error.Metadata;

        // Assert
        error.CollectCount.ShouldBe(1);
        second.ShouldBeSameAs(first);
        first["collected"].ShouldBe(1);
    }

    [Fact]
    public void Metadata_AddedAfterFirstRead_IsIncludedInTheNextRead()
    {
        // Arrange
        var error = new CollectingError();
        _ = error.Metadata;

        // Act
        error.Add("added", "value");

        // Assert
        error.Metadata["added"].ShouldBe("value");
        error.Metadata["collected"].ShouldBe(1);
        error.CollectCount.ShouldBe(2);
    }

    [Fact]
    public void Metadata_AddedWithTheCollectedKey_ReplacesTheCollectedValue()
    {
        // Arrange
        var error = new CollectingError();

        // Act
        error.Add("collected", "replaced");

        // Assert
        error.Metadata["collected"].ShouldBe("replaced");
        error.Metadata.Count.ShouldBe(1);
    }

    [Fact]
    public void AddMetadata_SameKeyTwice_ThrowsArgumentException()
    {
        // Arrange
        var error = new CollectingError();
        error.Add("key", 1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => error.Add("key", 2));
    }

    [Fact]
    public void Metadata_NothingCollectedOrAdded_IsTheSharedEmptyInstance()
    {
        // Act
        var first = new CodeError("code", "message").Metadata;
        var second = new CodeError("code", "message").Metadata;

        // Assert
        first.ShouldBeEmpty();
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task Metadata_ReadConcurrently_EveryReaderGetsTheSameInstance()
    {
        // Arrange
        var error = new CollectingError();
        using var start = new ManualResetEventSlim();
        var readers = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                start.Wait(TestContext.Current.CancellationToken);
                return error.Metadata;
            }, TestContext.Current.CancellationToken))
            .ToArray();

        // Act
        start.Set();
        var results = await Task.WhenAll(readers);

        // Assert
        results.ShouldAllBe(metadata => ReferenceEquals(metadata, results[0]));
        results[0]["collected"].ShouldBe(1);
    }

    [Fact]
    public void Metadata_AddedDuringABuild_IsNotLostToTheCache()
    {
        // Arrange: the first build adds an entry while it runs, as another thread's AddMetadata could
        var error = new AddingDuringBuildError();

        // Act
        var first = error.Metadata;
        var second = error.Metadata;
        var third = error.Metadata;

        // Assert: the overlapped build isn't cached, the next one is
        second["late"].ShouldBe(1);
        second.ShouldNotBeSameAs(first);
        third.ShouldBeSameAs(second);
        error.CollectCount.ShouldBe(2);
    }

    private sealed class AddingDuringBuildError() : OpenUrzednikError("adding", "message")
    {
        private bool _added;

        public int CollectCount { get; private set; }

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            CollectCount++;
            if (!_added)
            {
                _added = true;
                AddMetadata("late", 1);
            }
        }

        protected override OpenUrzednikException CreateException() => new TestException(Message);
    }

    // Counts CollectMetadata calls; exposes AddMetadata.
    private sealed class CollectingError() : OpenUrzednikError("collecting", "message")
    {
        private int _collectCount;

        public int CollectCount => Volatile.Read(ref _collectCount);

        public void Add(string key, object? value) => AddMetadata(key, value);

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            Interlocked.Increment(ref _collectCount);
            metadata["collected"] = 1;
        }

        protected override OpenUrzednikException CreateException() => new TestException(Message);
    }
}
