namespace MVFC.LongPolling.Tests;

public sealed class ChannelLockEntryTests
{
    [Fact]
    public void Dispose_ReleasesSemaphore_SubsequentWaitThrows()
    {
        // Arrange
        var entry = ChannelLockEntry.CreateWithRef();

        // Act
        entry.Dispose();

        // Assert — semaphore is disposed, so WaitAsync should throw
        var act = () => entry.WaitAsync(CancellationToken.None);
        act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CreateWithRef_SetsRefCountToOne_ReleaseReturnsTrue()
    {
        // Arrange
        var entry = ChannelLockEntry.CreateWithRef();
        await entry.WaitAsync(CancellationToken.None);

        // Act
        var isLast = entry.Release();

        // Assert
        isLast.Should().BeTrue();

        entry.Dispose();
    }

    [Fact]
    public async Task AddRef_IncrementsRefCount_ReleaseReturnsFalseUntilLast()
    {
        // Arrange
        var entry = ChannelLockEntry.CreateWithRef();
        entry.AddRef();
        await entry.WaitAsync(CancellationToken.None);

        // Act — first release (refCount goes from 2 to 1)
        var firstRelease = entry.Release();
        await entry.WaitAsync(CancellationToken.None);

        // second release (refCount goes from 1 to 0)
        var secondRelease = entry.Release();

        // Assert
        firstRelease.Should().BeFalse();
        secondRelease.Should().BeTrue();

        entry.Dispose();
    }
}
