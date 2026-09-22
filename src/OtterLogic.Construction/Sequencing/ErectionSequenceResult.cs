using System.Globalization;
using System.Text;

namespace OtterLogic.Construction;

/// <summary>
/// The order a model's elements go up in, and why each could go up when it did.
/// <para>
/// Elements are numbered in the order given, and every per-element array uses that
/// numbering. <see cref="Order"/> and <see cref="Sequence"/> are the same
/// permutation read both ways, because a Grasshopper definition wants both: the rank
/// of each element to colour or sort by, and the elements in rank order to play.
/// </para>
/// </summary>
public sealed class ErectionSequenceResult
{
    internal ErectionSequenceResult()
    {
    }

    /// <summary>The settings the sequence was planned with.</summary>
    public ErectionSequenceOptions Options { get; internal init; } = null!;

    /// <summary>Number of elements.</summary>
    public int ElementCount => Order.Length;

    /// <summary>Per element, its place in the sequence: 0 goes up first.</summary>
    public int[] Order { get; internal init; } = null!;

    /// <summary>The elements in the order they go up — the inverse of <see cref="Order"/>.</summary>
    public int[] Sequence { get; internal init; } = null!;

    /// <summary>
    /// Per element, the stage it goes up in: 0 is what rests on the ground. Stages
    /// never go backwards along the sequence — an element lifted after a higher stage
    /// has begun belongs to that stage, whatever its own level — so a stage is a
    /// contiguous run of the sequence and can be played as one.
    /// </summary>
    public int[] Stage { get; internal init; } = null!;

    /// <summary>Number of stages; the last holds anything with no route to a support.</summary>
    public int StageCount { get; internal init; }

    /// <summary>
    /// Per element, how many hand-overs its assembly stands from the ground, as the
    /// load paths read it: 0 rests on the supports. -1 when no support is reached.
    /// This is what the stages are cut from; it can sit below the stage where the
    /// order forced a wait.
    /// </summary>
    public int[] Level { get; internal init; } = null!;

    /// <summary>
    /// Per element, the earlier element it lands on: the placed neighbour that went up
    /// first. -1 for an element landing on a support, or starting a piece with no
    /// support to start from.
    /// </summary>
    public int[] RestsOn { get; internal init; } = null!;

    /// <summary>Per element, whether one of its joints is a support.</summary>
    public bool[] Grounded { get; internal init; } = null!;

    /// <summary>
    /// Elements that had nothing placed to land on when their turn came — the lowest of
    /// a piece no support reaches — in the order they were lifted. Each starts its
    /// piece in mid-air, which is what a crane would have to do too.
    /// </summary>
    public int[] Unsupported { get; internal init; } = null!;

    /// <summary>
    /// Elements sequenced only for completeness: drawn over another, or too short to be
    /// a piece. A duplicate goes up right after its original; a degenerate element goes
    /// up last.
    /// </summary>
    public int[] Skipped { get; internal init; } = null!;

    /// <summary>Things worth telling the user, in plain sentences.</summary>
    public IReadOnlyList<string> Notes { get; internal init; } = null!;

    /// <summary>The elements of each stage, in sequence order.</summary>
    public int[][] Stages()
    {
        var stages = new List<int>[StageCount];
        for (int s = 0; s < StageCount; s++)
            stages[s] = new List<int>();
        foreach (int e in Sequence)
            stages[Stage[e]].Add(e);
        return stages.Select(stage => stage.ToArray()).ToArray();
    }

    /// <summary>A short, readable account of the sequence.</summary>
    public string Report()
    {
        var text = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;
        text.AppendLine(ci, $"{ElementCount} elements in {StageCount} stage(s).");

        var stages = Stages();
        for (int s = 0; s < stages.Length; s++)
        {
            if (stages[s].Length == 0)
                continue;

            int grounded = stages[s].Count(e => Grounded[e]);
            text.Append(ci, $"  Stage {s}: {stages[s].Length} element(s)");
            if (grounded > 0)
                text.Append(ci, $", {grounded} on supports");
            text.AppendLine(ci, $" — first {stages[s][0]}, last {stages[s][^1]}.");
        }

        foreach (string note in Notes)
            text.AppendLine(note);

        return text.ToString().TrimEnd();
    }
}
