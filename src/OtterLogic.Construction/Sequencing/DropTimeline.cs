namespace OtterLogic.Construction;

/// <summary>
/// Where every piece is at one moment of a sequence played as time: not yet lifted,
/// on its way down, or landed.
/// <para>
/// One number drives it — time from 0, nothing up, to 1, everything landed — because
/// that is what a slider gives and what an animation frame asks for. Each piece has a
/// window of the timeline to fall through, windows start one after another in
/// sequence order, and as many pieces as the overlap allows are in the air at once. The
/// windows are sized so the last piece lands exactly at 1.
/// </para>
/// <para>
/// Progress through a window is eased as a fall: slow to leave the hook, fast to
/// land. That reads as gravity where a constant speed reads as a lift. The geometry
/// is left to the caller — this decides only how far along each piece is, so the
/// same timeline can drop, fade or grow the pieces.
/// </para>
/// </summary>
public static class DropTimeline
{
    /// <summary>
    /// Evaluates the timeline at one moment.
    /// </summary>
    /// <param name="order">Per piece, its place in the sequence, from <see cref="ErectionSequenceResult.Order"/>. Ties share a window.</param>
    /// <param name="time">0 before anything moves, 1 when everything has landed. Clamped.</param>
    /// <param name="overlap">How many pieces are in the air at once; at least one.</param>
    public static DropTimelineResult At(int[] order, double time, int overlap = 4)
    {
        if (order is null)
            throw new ArgumentNullException(nameof(order));
        if (overlap < 1)
            throw new ArgumentOutOfRangeException(nameof(overlap), overlap, "At least one piece must be in the air at a time.");
        if (double.IsNaN(time))
            throw new ArgumentException("Time must be a number between 0 and 1.", nameof(time));

        int n = order.Length;
        var progress = new double[n];
        if (n == 0)
            return new DropTimelineResult(progress, 0, 0);

        int last = 0;
        for (int i = 0; i < n; i++)
        {
            if (order[i] < 0)
                throw new ArgumentOutOfRangeException(nameof(order), order[i], $"Piece {i} has a negative place in the sequence.");
            last = Math.Max(last, order[i]);
        }

        double t = Math.Clamp(time, 0.0, 1.0);
        double slot = 1.0 / (last + overlap);

        int started = 0, landed = 0;
        for (int i = 0; i < n; i++)
        {
            double begin = order[i] * slot;
            double raw = Math.Clamp((t - begin) / (overlap * slot), 0.0, 1.0);
            progress[i] = raw * raw;
            if (t > begin) started++;
            if (raw >= 1.0) landed++;
        }

        return new DropTimelineResult(progress, started, landed);
    }
}

/// <summary>
/// How far along each piece is at one moment.
/// </summary>
/// <param name="Progress">Per piece: 0 not yet lifted, 1 landed, eased between.</param>
/// <param name="Started">How many pieces have left the hook.</param>
/// <param name="Landed">How many pieces are down.</param>
public sealed record DropTimelineResult(double[] Progress, int Started, int Landed)
{
    /// <summary>Whether the piece has left the hook — it is visible in the scene.</summary>
    public bool IsStarted(int piece) => Progress[piece] > 0.0;

    /// <summary>Whether the piece is down.</summary>
    public bool IsLanded(int piece) => Progress[piece] >= 1.0;
}
