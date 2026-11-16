using System.Collections.Generic;
using System.Linq;
using GameLovers.StatechartMachine;
using GameLovers.StatechartMachine.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartObservabilityTest
	{
		private readonly IStatechartEvent _go = new StatechartEvent("Go");

		[Test]
		// ADMIT: StatechartTopologyBuilder.BuildRegion must place each state only in the region that declared it;
		// the root factory also lists nested states, which would otherwise appear twice.
		// RCR: StatechartTopologyBuilder.cs BuildRegion — `if (state.Factory != factory)` → `if (false)` → RED (nested states listed in the root region). 2026-11-15
		public void RootRegion_NestedSetup_DescribesEachStateOnce()
		{
			var statechart = new Statechart("Topology", SetupNest);
			var root = statechart.RootRegion;
			var nest = root.States.Single(state => state.Name == "Nest");
			var inner = nest.Regions[0];

			Assert.AreEqual(new[] { "Initial", "Nest", "Final" }, root.States.Select(state => state.Name));
			Assert.AreEqual(StateKind.Nest, nest.Kind);
			Assert.AreEqual(new[] { "Inner Initial", "Inner", "Inner Final" }, inner.States.Select(state => state.Name));
			Assert.AreEqual("Nest/Inner", inner.States[1].Path);
			Assert.AreEqual(nest, inner.Owner);
			Assert.AreEqual(1, inner.Depth);
			Assert.AreEqual(TransitionKind.Event, inner.States[1].Transitions.Single().Kind);
			Assert.AreEqual("Go", inner.States[1].Transitions.Single().Event.Name);
			Assert.AreEqual(TransitionKind.Completion, nest.Transitions.Single().Kind);
		}

		[Test]
		// ADMIT: Statechart.GetActiveStates must walk into every region of an active split, parents first.
		// RCR: Statechart.cs AddActive — loop bound `split.Regions.Count` → `0` → RED (only "Split" active). 2026-11-15
		public void GetActiveStates_InsideSplit_ReturnsEveryRegionState()
		{
			var statechart = new Statechart("Split", factory =>
			{
				var initial = factory.Initial("Initial");
				var split = factory.Split("Split");

				initial.Transition().Target(split);
				split.Split(left => SetupRegion(left, "Left"), right => SetupRegion(right, "Right"));
			});
			var active = new List<StateNode>();

			statechart.Run();
			statechart.GetActiveStates(active);

			Assert.AreEqual(new[] { "Split", "Split/Left", "Split/Right" }, active.Select(state => state.Path));
			Assert.IsTrue(statechart.IsActive(active[2]));
		}

		[Test]
		// ADMIT: StateInternal.Trigger must publish exit, transition and enter steps in execution order after the
		// chart-level event step.
		// RCR: StateInternal.cs ExitSafely — remove the StateExited Trace call → RED (second step is the Transition). 2026-11-15
		public void Traced_EventTransition_PublishesOrderedSteps()
		{
			var statechart = new Statechart("Trace", factory =>
			{
				var initial = factory.Initial("Initial");
				var a = factory.State("A");
				var b = factory.State("B");

				initial.Transition().Target(a);
				a.Event(_go).Target(b);
			});
			var steps = new List<string>();

			statechart.Run();
			statechart.Traced += trace => steps.Add($"{trace.Kind} {trace.State?.Name}->{trace.Target?.Name}");
			statechart.Trigger(_go);

			Assert.AreEqual(new[]
			{
				"EventTriggered A->",
				"StateExited A->",
				"Transition A->B",
				"StateEntered B->"
			}, steps);
		}

		[Test]
		// ADMIT: Statechart.Emit must write each trace step to the console while LogsEnabled is on.
		// RCR: Statechart.cs Emit — remove `Debug.Log(trace.ToString());` → RED (event line missing). Also RED under the ExitSafely trace removal. 2026-11-15
		public void LogsEnabled_EventTransition_LogsTraceLines()
		{
			var statechart = new Statechart("Logs", factory =>
			{
				var initial = factory.Initial("Initial");
				var a = factory.State("A");
				var b = factory.State("B");

				initial.Transition().Target(a);
				a.Event(_go).Target(b);
			});

			statechart.Run();
			statechart.LogsEnabled = true;
			LogAssert.Expect(LogType.Log, "[Statechart 'Logs'] event 'Go'");
			LogAssert.Expect(LogType.Log, "[Statechart 'Logs'] exit A");
			LogAssert.Expect(LogType.Log, "[Statechart 'Logs'] A -> B on 'Go'");
			LogAssert.Expect(LogType.Log, "[Statechart 'Logs'] enter B");
			statechart.Trigger(_go);
		}

		[Test]
		// ADMIT: StatechartExport.ToMermaid must draw nested regions as a composite state and mark active states.
		// RCR: StatechartExport.cs ToMermaid — drop the `class ... active` line → RED (class line missing). 2026-11-15
		public void ToMermaid_NestedChart_DrawsCompositeAndActiveClass()
		{
			var statechart = new Statechart("Mermaid", SetupNest);

			statechart.Run();

			var inner = statechart.RootRegion.States[1].Regions[0].States[1];
			var mermaid = StatechartExport.ToMermaid(statechart);

			StringAssert.StartsWith("stateDiagram-v2", mermaid);
			StringAssert.Contains($"state \"Nest\" as s{statechart.RootRegion.States[1].Id} {{", mermaid);
			StringAssert.Contains($"s{inner.Id} --> [*] : Go", mermaid);
			StringAssert.Contains($"class s{statechart.RootRegion.States[1].Id},s{inner.Id} active", mermaid);
		}

		[Test]
		// ADMIT: Statechart.Dispose must remove the chart from the Editor registry the debugger lists.
		// RCR: Statechart.cs Dispose — remove `StatechartRegistry.Unregister(this);` → RED (chart still listed). 2026-11-15
		public void Dispose_RegisteredChart_LeavesRegistry()
		{
			var statechart = new Statechart("Registry", SetupNest);

			Assert.IsTrue(StatechartRegistry.GetEntries().Any(entry => entry.TryGetChart(out var c) && c == statechart));

			statechart.Dispose();

			Assert.IsFalse(StatechartRegistry.GetEntries().Any(entry => entry.TryGetChart(out var c) && c == statechart));
		}

		private void SetupNest(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("Final");

			initial.Transition().Target(nest);
			nest.Nest(inner =>
			{
				var innerInitial = inner.Initial("Inner Initial");
				var state = inner.State("Inner");
				var innerFinal = inner.Final("Inner Final");

				innerInitial.Transition().Target(state);
				state.Event(_go).Target(innerFinal);
			}).Target(final);
		}

		private static void SetupRegion(IStateFactory factory, string name)
		{
			var initial = factory.Initial($"{name} Initial");
			var state = factory.State(name);

			initial.Transition().Target(state);
		}
	}
}
