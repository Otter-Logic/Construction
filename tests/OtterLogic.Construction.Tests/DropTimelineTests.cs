using Xunit;

namespace OtterLogic.Construction.Tests;

public class DropTimelineTests
{
    private static readonly int[] Order = { 2, 0, 1, 3 };

    [Fact]
    public void NothingHasMoved_AtZero_AndEverythingHasLanded_AtOne()
    {
        var start = DropTimeline.At(Order, 0.0);
        Assert.All(start.Progress, p => Assert.Equal(0.0, p));
        Assert.Equal(0, start.Started);
        Assert.Equal(0, start.Landed);

        var end = DropTimeline.At(Order, 1.0);
        Assert.All(end.Progress, p => Assert.Equal(1.0, p));
        Assert.Equal(4, end.Started);
        Assert.Equal(4, end.Landed);
    }

    [Fact]
    public void PiecesLeaveTheHook_InSequenceOrder()
    {
        // Overlap 1: one piece at a time, each with a quarter of the timeline.
        var quarter = DropTimeline.At(Order, 0.26, overlap: 1);
        Assert.True(quarter.IsLanded(1));
        Assert.True(quarter.IsStarted(2));
        Assert.False(quarter.IsLanded(2));
        Assert.False(quarter.IsStarted(0));
        Assert.False(quarter.IsStarted(3));
    }

    [Fact]
    public void Progress_NeverGoesBackwards_AndFallsFasterAsItLands()
    {
        double[] previous = new double[Order.Length];
        for (int step = 1; step <= 100; step++)
        {
            var now = DropTimeline.At(Order, step / 100.0).Progress;
            for (int i = 0; i < now.Length; i++)
                Assert.True(now[i] >= previous[i]);
            previous = now;
        }

        // Eased as a fall: the first half of a window covers less than half the drop.
        var half = DropTimeline.At(new[] { 0 }, 0.5, overlap: 1);
        Assert.True(half.Progress[0] < 0.5);
    }

    [Fact]
    public void TimeOutsideTheRange_IsClamped()
    {
        Assert.All(DropTimeline.At(Order, -3.0).Progress, p => Assert.Equal(0.0, p));
        Assert.All(DropTimeline.At(Order, 7.0).Progress, p => Assert.Equal(1.0, p));
    }

    [Fact]
    public void BadInput_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DropTimeline.At(Order, 0.5, overlap: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DropTimeline.At(new[] { -1 }, 0.5));
        Assert.Throws<ArgumentException>(() => DropTimeline.At(Order, double.NaN));
        Assert.Empty(DropTimeline.At(Array.Empty<int>(), 0.5).Progress);
    }
}
