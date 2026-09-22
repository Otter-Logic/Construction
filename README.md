# Construction

Building the structure once it is designed, for
[OtterLogic](https://github.com/Otter-Logic/Rhino3D): the order its pieces can
go up in, and seeing that order happen.

```
Core  ->  MachineLearning  ->  Unsupervised  ->  StructuralEngine  ->  Construction
                                                 (reading a model:     (this repo: what
                                                  joints, members,      a site engineer
                                                  assemblies, loads)    does with it)
```

A domain toolkit, **sibling** to
[StructuralDesign](https://github.com/Otter-Logic/StructuralDesign) and
[Fabrication](https://github.com/Otter-Logic/Fabrication) — never a dependent of
either. What all three need — how a stick model is read as joints, members,
assemblies and load paths — lives below them in
[StructuralEngine](https://github.com/Otter-Logic/StructuralEngine), so a
sequence, a connection schedule and an analysis can never disagree about what
rests on what.

## What is here

**Erection Sequence** — lines and supports in; the order the lines go up in out,
with each line's stage, what it lands on and its level in the hand-over
hierarchy. Two rules, true of any erection, are all it uses:

- A piece is lifted only onto something already standing — a support, or a
  piece that went up before it. This is a hard rule.
- What carries goes up before what is carried: the column before the beam on
  it, the truss before the purlins on it. This is the order choices are made in
  whenever the first rule allows more than one.

"What carries what" is not guessed from names. It is the engine's reading of the
model — weight drained to the supports, hand-overs counted up from the ground —
and the level that gives each assembly is the first thing the choice is made on.
Among pieces at one level the lowest goes first, since cranes work upwards, and
among those the earliest drawn, so the answer never depends on anything the user
cannot see. A piece that no support reaches is still sequenced, from all of its
lowest elements upwards, once everything reachable is up — which is what a crane
would have to do — and the elements that started in mid-air are reported so the
missing support can be added. Duplicates go up with their original and
degenerate lines go last, so the output is always a whole permutation that can be
played end to end.

**Drop Timeline** — the sequence played as time. One number from 0 to 1 drives
it, because that is what a slider gives; each piece has its own window of the
timeline to fall through, windows start one after another in sequence order, and
a chosen number of pieces are in the air at once. Progress is eased as a fall.
The geometry is left to the caller, so the same timeline can drop, fade or grow
the pieces.

## Rules

- Depends on [StructuralEngine](https://github.com/Otter-Logic/StructuralEngine),
  which carries [Unsupervised](https://github.com/Otter-Logic/Unsupervised),
  [MachineLearning](https://github.com/Otter-Logic/MachineLearning),
  [Graphs](https://github.com/Otter-Logic/Graphs) and
  [Core](https://github.com/Otter-Logic/Core) transitively. Never another domain
  toolkit, never an adaptor.
- No UI. No Grasshopper. The components live in
  [Rhino3D](https://github.com/Otter-Logic/Rhino3D), under the **Construction**
  section; this repo does not know they exist.

## Build and test

```
dotnet build OtterLogic.slnx
dotnet test OtterLogic.slnx
```

Arithmetic over coordinate arrays — no Rhino, no licence needed, runs anywhere.

Clone [Core](https://github.com/Otter-Logic/Core),
[Graphs](https://github.com/Otter-Logic/Graphs),
[MachineLearning](https://github.com/Otter-Logic/MachineLearning),
[Unsupervised](https://github.com/Otter-Logic/Unsupervised) and
[StructuralEngine](https://github.com/Otter-Logic/StructuralEngine) as sibling
folders and the project references resolve against your working copy; without
them the build falls back to the published packages.

## License

[MIT](LICENSE).
