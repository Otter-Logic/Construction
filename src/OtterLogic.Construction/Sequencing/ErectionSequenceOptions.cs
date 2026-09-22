namespace OtterLogic.Construction;

/// <summary>
/// Settings for <see cref="ErectionSequence.Plan"/>.
/// <para>
/// Deliberately few. The sequence is read from what is true of every structure — the
/// ground comes first, a piece needs something to land on — and the only thing the
/// user can legitimately know better than the model is how close two points must be
/// to count as one joint.
/// </para>
/// </summary>
public sealed record ErectionSequenceOptions
{
    /// <summary>
    /// Points closer than this are one point. The Rhino document's absolute tolerance
    /// is the right value; a millimetre is the default because that is what most
    /// documents use.
    /// </summary>
    public double Tolerance { get; init; } = 0.001;

    /// <summary>
    /// How close two element ends must be to weld into one joint. Ten tolerances by
    /// default, the same as the Insight engine uses, so a model the engine reads as
    /// joined is sequenced as joined — the two must never disagree about what rests
    /// on what.
    /// </summary>
    public double? JoinDistance { get; init; }

    internal double Join => JoinDistance ?? 10.0 * Tolerance;

    public void Validate()
    {
        if (!double.IsFinite(Tolerance) || Tolerance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(Tolerance), Tolerance, "Tolerance must be a positive distance.");

        if (JoinDistance is { } join && (!double.IsFinite(join) || join < Tolerance))
            throw new ArgumentOutOfRangeException(nameof(JoinDistance), join,
                $"Join distance must be at least the tolerance ({Tolerance}); leave it unset for ten times the tolerance.");
    }
}
