using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartTest
	{
		private IMockCaller _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		// ADMIT: InitialState.Exit fans out every registered OnExit action, so the initial state's exit hook
		// runs on the way to the final state rather than being skipped.
		// RCR: InitialState.cs Exit — change the fan-out loop bound to `i < 0` → RED
		// (_caller.InitialOnExitCall(0) is never received).
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupSimpleFlow);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: InitialState.Validate rejects an initial state with NO transition at all — the null-conditional
		// in `_transition?.TargetState == null` is what covers this half; the sibling test below covers the
		// transition-exists-but-has-no-target half of the same guard.
		// RCR: InitialState.cs Validate — change the guard to `_transition != null && _transition.TargetState
		// == null` (no longer fires when _transition itself is null) → RED (no MissingMemberException). The
		// sibling below stays green under this edit.
		public void InitialState_MissingTransition_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");
			}));
		}

		[Test]
		// ADMIT: InitialState.Validate also rejects a transition that was declared but never given a Target —
		// the `.TargetState == null` half of the same guard the sibling test above exercises.
		// RCR: InitialState.cs Validate — change the guard to `_transition == null` (no longer inspects
		// TargetState) → RED (no MissingMemberException). The sibling above stays green under this edit.
		public void InitialState_TransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0));
			}));
		}

		[Test]
		// ADMIT: InitialState.Validate rejects an initial state whose transition targets itself, which would
		// otherwise build a chart that spins on entry.
		// RCR: InitialState.cs Validate — change `if (_transition.TargetState.Id == Id)` to `if (false)` → RED
		// (no InvalidOperationException).
		public void InitialState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("Final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(initial);
			}));
		}

		[Test]
		// ADMIT: InitialState.Transition() rejects a second Transition() call, so an initial state cannot end up
		// with an ambiguous pair of outgoing transitions (the second would silently replace the first).
		// RCR: InitialState.cs Transition — change `if (_transition != null)` to `if (false)` → RED (no
		// InvalidOperationException; the second transition just overwrites the first).
		public void InitialState_MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("Final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
				initial.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			}));
		}

		[Test]
		// ADMIT: Statechart's constructor rejects a setup that never declared an initial state, rather than
		// leaving _currentState null for Run() to dereference later.
		// RCR: Statechart.cs ctor — change `if (_stateFactory.InitialState == null)` to `if (false)` → RED (no
		// MissingMemberException at construction).
		public void NoInitialState_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var final = factory.Final("final");
			}));
		}

		[Test]
		// ADMIT: StateFactory.Initial rejects a second initial state in the same region instead of silently
		// replacing the first, which would make the chart's entry point depend on declaration order.
		// RCR: StateFactory.cs Initial — change `if (InitialState != null)` to `if (false)` → RED (no
		// InvalidOperationException).
		public void MultipleInitialStates_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				SetupSimpleFlow(factory);

				var initial1 = factory.Initial("Initial1");
			}));
		}

		[Test]
		// ADMIT: StateFactory.Final rejects a second final state in the same region, for the same reason as
		// Initial above — the survivor would otherwise depend on declaration order.
		// RCR: StateFactory.cs Final — change `if (FinalState != null)` to `if (false)` → RED (no
		// InvalidOperationException).
		public void MultipleFinalState_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				SetupSimpleFlow(factory);

				var final2 = factory.Final("final2");
			}));
		}
		
		private void SetupSimpleFlow(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}