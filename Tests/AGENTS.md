# GameLovers.Statechart Tests — AI Agent Guide

This file contains testing conventions for the `com.gamelovers.statechart` package. It is the source of truth when reading, editing, or creating test files under `Tests/`.

For runtime architecture, gotchas, and package-level context, see the parent [`AGENTS.md`](../AGENTS.md).

§1 and §2 are shared verbatim across every GameLovers package. A change to either must be applied to all six `Tests/AGENTS.md` files in the same working session, one commit per submodule.

## 1. ADMIT — Test Admission Test

A proposed test is admitted only if all five answers are YES. Record the first two
as comments on the test itself.

| | Question |
|---|---|
| **A1 DEFECT** | Can you name the defect in one sentence, referencing a production file and symbol? "It could break" is not a defect. |
| **A2 RED** | Can you name the exact production edit — one line or one branch, identified by `file` + `symbol` — that makes this test fail? If no such single edit exists, the test pins nothing. |
| **A3 PACKAGE** | Does every assertion read a value this package computed? Reject assertions on `new X() != new X()`, `!= null` on a freshly constructed object, default struct/enum values, or anything the C# spec or the Unity engine already guarantees. |
| **A4 CHEAPEST** | Is this the cheapest tier that covers the defect? EditMode beats PlayMode; a `[TestCase]` row on an existing fixture beats a new `[Test]`; a new `[Test]` beats a new fixture. Grep before writing. |
| **A5 UNIQUE** | Does no existing test already fail on the A2 edit? Grep the symbol under test across `Tests/` first. |

**A5-bis — inherited-type coverage.** Before proposing a fixture for a type that
derives from or wraps another tested type, grep `Tests/` for the derived type's
name and for paired `[SetUp]` fields. Base-and-derived pairs are tested jointly in
the base's fixture unless the derived type adds new public surface.

**Two mechanical disqualifiers** — violate one and the test is rejected:

- **D1 — tautology.** If the only assertion is `Assert.DoesNotThrow`,
  `Assert.IsNotNull`, or a disjunction of `Contains(...)` substrings, the test
  fails A2 unless you write down what *would* throw, be null, or not match. A
  substring disjunction that includes a string the input itself embeds is
  unfalsifiable by construction.
- **D2 — name/body contract.** The test name is a claim. If deleting the
  production feature the name mentions leaves the test green, the name is a lie.

**Smoke exemption, by directory.** Fixtures under `Smoke/` are exempt from A1 and
A2 and may assert construction-without-throwing only. Their defect class is "the
assembly no longer loads / bootstrap regressed", which is real and not expressible
otherwise. The exemption is by directory, not by assertion shape — a Unit test
that only asserts `IsNotNull` is still rejected.

## 2. RCR — Revert and Confirm Red

> Every new or strengthened test must be observed failing, once, against a
> one-line production revert, before it is committed.

Line coverage proves a line executed. It does not prove any test would notice if
that line were wrong. RCR is the cheap substitute for mutation testing, and it is
what makes a coverage number trustworthy.

**Procedure** (~90 seconds per test):

1. Write the test. Run it. Green.
2. Apply the A2 edit — invert the comparison, delete the guard clause, return
   early, comment out the one line. **One line only**: a broad deletion proves
   nothing, because it would also "fail" a tautological test via a compile error.
3. Run only that test. It must be **RED**, and the failure message must name the
   thing you broke. A red-by-`NullReferenceException` does not count — that is the
   test crashing, not asserting.
4. `git checkout -- <production file>`. Re-run. Green.
5. Record the mutation in the test's header comment.

**Recording format** — on the test, not in a separate ledger. A ledger rots the
moment a test is renamed; a comment travels with the test, appears in every diff
that touches it, and lets a reviewer re-run the mutation in 30 seconds.

```csharp
[Test]
// ADMIT: <one-sentence defect, naming a production file and symbol>
// RCR:   <file> <symbol> — <the one-line mutation> → RED (<what the failure says>). <YYYY-MM-DD>
public void Method_Condition_ExpectedResult()
```

**Anchor on `file` + `symbol`, never `file:line`.** Line numbers rot on the first
unrelated edit above them — a stale `:474` pointing at a method that moved to `:464`
sends the next reader to the wrong code and quietly destroys the comment's value.

**Budget: four lines is the target, six is the ceiling.** One sentence of ADMIT,
one of RCR, wrapped. This obeys the repo-wide rule in the root `AGENTS.md`
(§ Code comments): *"One sentence usually suffices. Multi-paragraph rationale is a
smell."* Anything past the ceiling belongs in the commit body or `docs/`, not on the
test. Two things in particular must NOT appear here:
- **Change narration.** *"An earlier version of this test was a tautology"* is diff
  context; the root `AGENTS.md` forbids it outright. A comment states the code's
  permanent condition, not its history. Put it in the commit message.
- **Investigation transcript.** The empirical detail that convinced *you* is not
  what the next reader needs. They need the mutation and the expected failure.

The one extension worth its lines is a **negative** result: naming a nearby edit
that looks like a valid mutation but is NOT one (because it is already guarded, or
because it reddens a sibling test instead). That stops the next reader repeating a
dead end, and it cannot be recovered from the code.

Also add one line per new test to the commit body: `RCR: <TestName> ← <file> <symbol> <mutation>`.
That makes `git log --grep=RCR` the audit surface.

**UNFALSIFIABLE — the one honest exemption.** Some correct tests provably have no
one-line mutation. The commonest case is **double-guarded validation**: an
unconfigured object trips two independent guards, so disabling either leaves the
other throwing. Deleting such a test would lose real coverage, so it is exempt —
but only on the same terms as §13, never as a shrug:

```csharp
// RCR: none exists — <input> trips both <guard A> and <guard B>; disabling either
// leaves the other throwing (verified). Double-covered, not single-line falsifiable.
```

The reason must be falsifiable and must record that a mutation was actually tried
and observed green. "Couldn't find one" is not a reason — that is an unfinished RCR,
not an exemption.

**Verdicts for a test that resists mutation.** Work out which of three it is; they
have different answers:

| Finding | Test | Action |
|---|---|---|
| **A5 duplicate** — the only mutation that reddens it already belongs to a sibling | pins nothing new | **Delete**, naming the surviving sibling in the commit body |
| **D2 overclaim** — the name promises behaviour the body cannot detect | name is a lie | **Strengthen the assertion**, or rename to what it actually checks |
| **UNFALSIFIABLE** — real behaviour, but double-guarded or otherwise unbreakable one line at a time | valid | **Keep**, with the exemption comment above |

Prove the class before acting. An A5 duplicate is confirmed when the sibling's
mutation is observed reddening both; a D2 overclaim is confirmed when the mutation
the name implies leaves the test green.

**Two consequences, stated so RCR does not become theatre:**

- A test with no `// RCR:` line — and no UNFALSIFIABLE exemption — is not trusted
  coverage. In an audit it is a suspect by default.
- **Benchmarks are included, inverted:** a performance test must be observed
  *changing its number* when the measured operation is removed from the measured
  body. A benchmark whose measured region does not contain the workload is a
  tautology in `Measure` clothing.

## 3. Placement Rules

Not yet documented — this package has not been through a test audit. There is a
single `Tests/Editor/` assembly and no PlayMode tests today. See the parent
[`AGENTS.md`](../AGENTS.md) for runtime architecture until this section is filled in.

## 4. Namespace and Suppression

Not yet documented — see existing files under `Tests/Editor/` for the current
(unaudited) convention.

## 5. Naming

Not yet documented.

## 6. Mock / Helper Types

Not yet documented. Note: NSubstitute is referenced in this package's test asmdef.

## 7. Black-Box / Reflection Policy

Not yet documented. Note: there is no `InternalsVisibleTo` grant from `Runtime/`
to the test assembly in this package as of this writing — verify before assuming
internal access is available.

## 8. Fields and Setup

Not yet documented.

## 9. Assertion Style

Not yet documented.

## 10. PlayMode Test Cleanup

None — this package has no PlayMode assembly.

## 11. Performance Tests

Not yet documented. No dedicated performance fixtures exist today.

## 12. Test Directory Layout

| Directory | Contents |
|---|---|
| `Tests/Editor/` | The package's entire automated suite (EditMode only) |

## 13. Coverage Register

Every untested symbol worth naming is either ACCEPTED (justified — do not
re-report) or OPEN (a real gap, owed a test). An untested symbol in neither state
is an audit finding.

An ACCEPTED row needs one of exactly three falsifiable reasons:
- **(i) no branching** — zero conditionals, so there is no behaviour to pin.
- **(ii) engine-owned** — the assertion would target Unity/OS behaviour
  (`[DllImport]`, `AndroidJavaObject`, Addressables statics).
- **(iii) harness-impossible** — the state cannot be fabricated in EditMode or
  PlayMode, **with the specific blocker named**.

"Low value", "hard to test", and "covered by manual QA" are NOT valid reasons. If
none of the three applies, the row is OPEN.

ACCEPTED is dated and **expires on edit**: if the symbol's file changes, the
reason is re-checked in that PR. A `(i) no branching` row is void the moment
someone adds an `if`.

OPEN is the only place a deletion may park coverage. A test removed for weakness
either had a stronger sibling (named in the commit body) or leaves an OPEN row.
The count of OPEN rows is the honest coverage-debt number.

| Symbol (file:line) | State | Reason / Owed | Recorded |
|---|---|---|---|

Empty — this package has not yet been through a coverage audit. Do not assume an
untested symbol here is accepted; it is simply unreviewed.

## 14. Update Policy

Update this file when this package is next audited for test coverage, and when
§1/§2 change upstream (propagate to all six `Tests/AGENTS.md` files in the same
session).
