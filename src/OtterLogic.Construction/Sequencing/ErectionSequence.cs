using OtterLogic.StructuralEngine;

namespace OtterLogic.Construction;

/// <summary>
/// The order a structure's elements can go up in, read from its geometry and its
/// supports and nothing else.
/// <para>
/// Two things are true of every erection, and they are all this uses. A piece can be
/// lifted only onto something already standing — a support, or a piece that went up
/// before it. And what carries goes up before what is carried: the column before the
/// beam that sits on it, the truss before the purlins that rest on it. The first is a
/// hard rule. The second is the order in which choices are made whenever the first
/// rule allows more than one.
/// </para>
/// <para>
/// "What carries what" is not guessed from names. It is the same reading of the model
/// Section Groups makes — points welded into joints, lines chained into physical
/// members, members triangulated into assemblies, and every element's weight drained
/// to the supports by <see cref="LoadPaths"/>. The level that gives each assembly (0
/// rests on the ground, 1 rests on something at 0, and so on) is the first thing the
/// choice is made on. Among elements at one level the lowest goes first, since
/// cranes work upwards, and among those the earliest drawn, so the answer never
/// depends on anything the user cannot see.
/// </para>
/// <para>
/// A piece that no support reaches — a separate part of the model, or a model with
/// no supports at all — is still sequenced, from its lowest element upwards, once
/// everything reachable is up. That is what a crane would have to do, and it keeps
/// the output a full permutation so it can be played end to end; the element that
/// started in mid-air is reported so the user can add the support they forgot.
/// </para>
/// </summary>
public static class ErectionSequence
{
    /// <summary>
    /// Plans the order a model's line elements go up in.
    /// </summary>
    /// <param name="lineStarts">n x 3, the start of each line element.</param>
    /// <param name="lineEnds">n x 3, the end of each line element, in the same order.</param>
    /// <param name="supports">m x 3 support points; null or empty when the model has none.</param>
    /// <param name="options">Settings; null for the defaults.</param>
    public static ErectionSequenceResult Plan(
        double[,] lineStarts, double[,] lineEnds, double[,]? supports = null, ErectionSequenceOptions? options = null)
    {
        options ??= new ErectionSequenceOptions();
        options.Validate();

        var (starts, ends) = ModelInput.CheckLines(lineStarts, lineEnds);
        if (supports is not null)
            ModelInput.CheckPoints(supports, nameof(supports));

        int n = starts.GetLength(0);
        if (n == 0)
            throw new ArgumentException("Nothing to sequence: give at least one line.", nameof(lineStarts));

        var none = Array.Empty<double[,]>();
        var structure = StructureGraph.Build(starts, ends, none, supports, options.Join);
        var geometry = ElementGeometry.Measure(starts, ends, none);
        var members = PhysicalMembers.Read(structure, geometry);
        var assemblies = Assemblies.Read(structure, members);
        var paths = LoadPaths.Trace(structure, geometry, members, assemblies);

        var level = new int[n];
        for (int e = 0; e < n; e++)
            level[e] = paths.Traced ? paths.Level[assemblies.Of[members.Of[e]]] : -1;

        var grounded = new bool[n];
        for (int e = 0; e < n; e++)
            grounded[e] = structure.ElementJoints[e].Any(j => structure.Supported[j]);

        // Which elements each joint touches, so placing one joint offers its neighbours.
        var incident = new List<int>[structure.Joints.Length];
        for (int j = 0; j < incident.Length; j++)
            incident[j] = new List<int>();
        for (int e = 0; e < n; e++)
            foreach (int j in structure.ElementJoints[e])
                incident[j].Add(e);

        var duplicates = new List<int>?[n];
        for (int e = 0; e < n; e++)
            if (structure.DuplicateOf[e] >= 0)
                (duplicates[structure.DuplicateOf[e]] ??= new List<int>()).Add(e);

        bool Lifts(int e) => !structure.Degenerate[e] && structure.DuplicateOf[e] < 0;

        // The choice among what can be lifted: lowest level, then lowest, then first drawn.
        var low = new double[n];
        for (int e = 0; e < n; e++)
            low[e] = structure.ElementJoints[e].Min(j => structure.Joints[j].Z);
        int Rank(int e) => level[e] < 0 ? int.MaxValue : level[e];
        var candidates = new SortedSet<int>(Comparer<int>.Create((a, b) =>
        {
            int byLevel = Rank(a).CompareTo(Rank(b));
            if (byLevel != 0) return byLevel;
            int byHeight = low[a].CompareTo(low[b]);
            return byHeight != 0 ? byHeight : a.CompareTo(b);
        }));

        var order = Enumerable.Repeat(-1, n).ToArray();
        var restsOn = Enumerable.Repeat(-1, n).ToArray();
        var sequence = new List<int>(n);
        var unsupported = new List<int>();
        var offered = new bool[n];
        int lifted = 0;

        void Offer(int joint)
        {
            foreach (int e in incident[joint])
                if (!offered[e] && order[e] < 0 && Lifts(e))
                {
                    offered[e] = true;
                    candidates.Add(e);
                }
        }

        void Place(int e, int onto)
        {
            order[e] = sequence.Count;
            sequence.Add(e);
            restsOn[e] = onto;
            lifted++;
            if (onto < 0 && !grounded[e])
                unsupported.Add(e);
            foreach (int j in structure.ElementJoints[e])
                Offer(j);

            // What was drawn twice goes up with its original: it is one piece in the world.
            if (duplicates[e] is { } twins)
                foreach (int twin in twins)
                {
                    order[twin] = sequence.Count;
                    sequence.Add(twin);
                    restsOn[twin] = e;
                }
        }

        // What lands on a support has the ground to rest on; otherwise it rests on
        // the neighbour that went up first.
        int Bearer(int e)
        {
            if (grounded[e])
                return -1;

            int first = -1;
            foreach (int j in structure.ElementJoints[e])
                foreach (int other in incident[j])
                    if (other != e && order[other] >= 0 && (first < 0 || order[other] < order[first]))
                        first = other;
            return first;
        }

        for (int j = 0; j < structure.Supported.Length; j++)
            if (structure.Supported[j])
                Offer(j);

        int liftable = Enumerable.Range(0, n).Count(Lifts);
        while (lifted < liftable)
        {
            if (candidates.Count == 0)
            {
                // Nothing standing to land on. Whatever is left, its lowest joints are
                // treated as the ground it would be propped from: every element down at
                // that level is offered, so a portal starts from both legs rather than
                // hanging its beam off the first.
                var left = Enumerable.Range(0, n).Where(e => order[e] < 0 && Lifts(e)).ToArray();
                int nearest = left.Min(Rank);
                var pool = left.Where(e => Rank(e) == nearest).ToArray();
                double floor = pool.Min(e => low[e]);
                foreach (int e in pool)
                    foreach (int j in structure.ElementJoints[e])
                        if (structure.Joints[j].Z <= floor + options.Join)
                            Offer(j);
                continue;
            }

            int next = candidates.Min;
            candidates.Remove(next);
            Place(next, Bearer(next));
        }

        // Degenerate elements last: they are not pieces, but the permutation must be whole.
        var skipped = new List<int>();
        for (int e = 0; e < n; e++)
            if (structure.DuplicateOf[e] >= 0)
                skipped.Add(e);
        for (int e = 0; e < n; e++)
            if (order[e] < 0)
            {
                skipped.Add(e);
                order[e] = sequence.Count;
                sequence.Add(e);
            }

        // Stages: the level, never going backwards along the sequence.
        int unreachedStage = paths.Levels + 1;
        var stage = new int[n];
        int running = 0;
        foreach (int e in sequence)
        {
            int own = level[e] < 0 ? unreachedStage : level[e];
            running = Math.Max(running, own);
            stage[e] = running;
        }

        var notes = new List<string>();
        if (!structure.HasSupports)
            notes.Add("No supports given: the sequence starts from the lowest element and works upwards, with no notion of what carries what.");
        else if (!paths.Traced)
            notes.Add("No element reaches a support, so the sequence starts from the lowest element and works upwards.");
        else if (!paths.Converged)
            notes.Add("The load paths stopped short of converging; levels may be approximate.");

        if (structure.StrandedSupports.Length > 0)
            notes.Add($"{structure.StrandedSupports.Length} support(s) touch no element end and were ignored: " + Some(structure.StrandedSupports) + ".");

        int unreached = Enumerable.Range(0, n).Count(e => level[e] < 0 && Lifts(e));
        if (paths.Traced && unreached > 0)
            notes.Add($"{unreached} element(s) have no route to a support and go up last, in stage {unreachedStage}.");

        if (unsupported.Count > 0 && structure.HasSupports)
            notes.Add($"{unsupported.Count} element(s) started a piece with nothing to land on: " + Some(unsupported)
                + ". A support is probably missing there.");

        int twins = structure.DuplicateOf.Count(d => d >= 0);
        if (twins > 0)
            notes.Add($"{twins} element(s) are drawn over another and go up with it.");

        int degenerate = structure.Degenerate.Count(d => d);
        if (degenerate > 0)
            notes.Add($"{degenerate} element(s) are too short to be a piece and go up last.");

        return new ErectionSequenceResult
        {
            Options = options,
            Order = order,
            Sequence = sequence.ToArray(),
            Stage = stage,
            StageCount = stage.Max() + 1,
            Level = level,
            RestsOn = restsOn,
            Grounded = grounded,
            Unsupported = unsupported.ToArray(),
            Skipped = skipped.ToArray(),
            Notes = notes,
        };
    }

    private static string Some(IReadOnlyCollection<int> indices)
        => string.Join(", ", indices.Take(10)) + (indices.Count > 10 ? ", ..." : "");
}
