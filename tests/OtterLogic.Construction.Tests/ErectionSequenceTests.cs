using Xunit;

namespace OtterLogic.Construction.Tests;

/// <summary>
/// The sequence is tested on what is true of any erection: every piece lands on
/// something standing, what carries goes up before what it carries, the ground comes
/// first, the answer does not depend on which way the model faces, and every element
/// goes up exactly once however badly the model is drawn.
/// </summary>
public class ErectionSequenceTests
{
    [Fact]
    public void EveryElement_GoesUpExactlyOnce()
    {
        var result = new Models().Frame(3, 2, 2).Sequence();

        Assert.Equal(Enumerable.Range(0, result.ElementCount), result.Order.OrderBy(o => o));
        for (int e = 0; e < result.ElementCount; e++)
            Assert.Equal(e, result.Sequence[result.Order[e]]);
    }

    [Fact]
    public void EveryPiece_LandsOnSomethingStanding()
    {
        var model = new Models().Frame(3, 2, 2);
        var result = model.Sequence();

        for (int e = 0; e < result.ElementCount; e++)
        {
            if (result.Grounded[e])
            {
                Assert.Equal(-1, result.RestsOn[e]);
                continue;
            }

            int bearer = result.RestsOn[e];
            Assert.True(bearer >= 0, $"element {e} landed on nothing");
            Assert.True(result.Order[bearer] < result.Order[e], $"element {e} landed on {bearer}, which went up after it");
        }

        Assert.Empty(result.Unsupported);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void WhatCarries_GoesUpBeforeWhatItCarries()
    {
        var model = new Models().Frame(2, 2, 1);
        var result = model.Sequence();

        // Every column stands on the ground; every beam rests on columns.
        for (int e = 0; e < result.ElementCount; e++)
            Assert.Equal(model.Vertical[e] ? 0 : 1, result.Level[e]);

        int lastColumn = Enumerable.Range(0, result.ElementCount).Where(e => model.Vertical[e]).Max(e => result.Order[e]);
        int firstBeam = Enumerable.Range(0, result.ElementCount).Where(e => !model.Vertical[e]).Min(e => result.Order[e]);
        Assert.True(lastColumn < firstBeam);

        Assert.Equal(2, result.StageCount);
        var stages = result.Stages();
        Assert.All(stages[0], e => Assert.True(model.Vertical[e]));
        Assert.All(stages[1], e => Assert.False(model.Vertical[e]));
    }

    [Fact]
    public void Stages_NeverGoBackwardsAlongTheSequence()
    {
        // Beams resting on the frame's beams, and one resting on those.
        var model = new Models().Frame(2, 2, 1);
        model.Line(3, 0, 4, 3, 6, 4);
        model.Line(9, 0, 4, 9, 6, 4);
        model.Line(3, 3, 4, 9, 3, 4);

        var result = model.Sequence();

        Assert.Equal(4, result.StageCount);
        for (int i = 1; i < result.Sequence.Length; i++)
            Assert.True(result.Stage[result.Sequence[i]] >= result.Stage[result.Sequence[i - 1]]);

        // The stages here are exactly the levels: nothing had to wait.
        for (int e = 0; e < result.ElementCount; e++)
            Assert.Equal(result.Level[e], result.Stage[e]);
    }

    [Fact]
    public void LowerGoesFirst_WithinAStage()
    {
        var model = new Models().Frame(1, 1, 3);
        var result = model.Sequence();

        // Column pieces are one member and so one level; the ground-floor pieces go up first.
        var columns = Enumerable.Range(0, result.ElementCount).Where(e => model.Vertical[e]).ToArray();
        Assert.All(columns, e => Assert.Equal(0, result.Level[e]));

        for (int storey = 1; storey < 3; storey++)
        {
            int lowerLast = columns.Where(e => e % 3 == storey - 1).Max(e => result.Order[e]);
            int upperFirst = columns.Where(e => e % 3 == storey).Min(e => result.Order[e]);
            Assert.True(lowerLast < upperFirst, $"storey {storey} column went up before storey {storey - 1} was complete");
        }
    }

    [Fact]
    public void TheSequence_DoesNotDependOnWhichWayTheModelFaces()
    {
        var model = new Models().Frame(3, 2, 2);
        var turned = model.TurnedAboutVertical(37.0);

        Assert.Equal(model.Sequence().Order, turned.Sequence().Order);
    }

    [Fact]
    public void ADisconnectedPiece_GoesUpLast_FromItsLowestElement()
    {
        var model = new Models().Frame(1, 1, 1);
        // A portal beside the frame with no support under it.
        int leftLeg = model.Line(20, 0, 0, 20, 0, 4);
        int rightLeg = model.Line(26, 0, 0, 26, 0, 4);
        int top = model.Line(20, 0, 4, 26, 0, 4);

        var result = model.Sequence();

        int lastOfFrame = Enumerable.Range(0, leftLeg).Max(e => result.Order[e]);
        Assert.True(lastOfFrame < result.Order[leftLeg]);
        Assert.True(result.Order[leftLeg] < result.Order[rightLeg]);
        Assert.True(result.Order[rightLeg] < result.Order[top]);

        Assert.Equal(new[] { leftLeg, rightLeg }, result.Unsupported);
        Assert.Equal(leftLeg, result.RestsOn[top]);
        Assert.Equal(-1, result.Level[top]);
        Assert.Equal(result.StageCount - 1, result.Stage[top]);
        Assert.Contains(result.Notes, note => note.Contains("no route to a support"));
    }

    [Fact]
    public void WithoutSupports_TheLowestGoesFirst()
    {
        var model = new Models();
        int top = model.Line(0, 0, 4, 6, 0, 4);
        int left = model.Line(0, 0, 0, 0, 0, 4);
        int right = model.Line(6, 0, 0, 6, 0, 4);

        var result = model.Sequence();

        Assert.Equal(0, result.Order[left]);
        Assert.Equal(1, result.Order[right]);
        Assert.Equal(2, result.Order[top]);
        Assert.Equal(1, result.StageCount);
        Assert.Equal(new[] { left, right }, result.Unsupported);
        Assert.Contains(result.Notes, note => note.StartsWith("No supports"));
    }

    [Fact]
    public void ADuplicate_GoesUpWithItsOriginal_AndADegenerateLast()
    {
        var model = new Models().Frame(1, 1, 1);
        int twin = model.Line(0, 0, 0, 0, 0, 4);
        int dot = model.Line(3, 3, 4, 3, 3, 4.0001);

        var result = model.Sequence();

        Assert.Equal(result.Order[0] + 1, result.Order[twin]);
        Assert.Equal(0, result.RestsOn[twin]);
        Assert.Equal(result.ElementCount - 1, result.Order[dot]);
        Assert.Equal(new[] { twin, dot }, result.Skipped);
    }

    [Fact]
    public void BadInput_IsRefusedWithAReason()
    {
        Assert.Throws<ArgumentException>(() => ErectionSequence.Plan(new double[0, 3], new double[0, 3]));
        Assert.Throws<ArgumentException>(() => ErectionSequence.Plan(new double[2, 3], new double[1, 3]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Models().Frame(1, 1, 1).Sequence(new ErectionSequenceOptions { Tolerance = 0.0 }));
    }
}
