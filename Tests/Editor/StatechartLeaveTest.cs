using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartLeaveTest
	{		
		private readonly IStatechartEvent _event = new StatechartEvent("Event");

		private IMockCaller _caller;
		private NestedStateData _nestedStateData;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_nestedStateData = new NestedStateData(SetupNest);
		}

		[Test]
		// ADMIT: LeaveState.Enter fans out its OnEnter actions, so a nested region's leave state still runs its
		// entry hook on the way back out to the layer above.
		// RCR: LeaveState.cs Enter — change the fan-out loop bound to `i < 0` → RED (StateOnEnterCall(0) never
		// received). Also reddens the two siblings below, which assert the same hook.
		public void SimpleNestTest()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates hands control to the LEAVE state's own transition, not the split's,
		// so leaving a region runs the leave's OnTransition and skips the split's completion transition.
		// RCR: SplitState.cs ProcessInnerStates — change `: leaveState.LeaveTransition;` to `: _transition;` → RED
		// (OnTransitionCall(2) fires instead of (1)). Also reddens the nest and only-leave siblings.
		public void SimpleSplitTest()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates detects a leave among its inner states and lets it win over the
		// split's own completion path; without that detection the split completes normally instead of leaving.
		// RCR: SplitState.cs ProcessInnerStates — disable the `is LeaveState state` capture → RED (leaveState stays
		// null, so the split's own transition runs and OnTransitionCall(2) fires). Also reddens the two siblings.
		public void SplitState_OnlyLeaveInnerStates_LeaveFirstState()
		{
			var statechart = new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);
				var nestedStateData = _nestedStateData = new NestedStateData(factory => SetupLeave(factory, final));

				split.OnEnter(() => _caller.StateOnEnterCall(1));
				split.Split(_nestedStateData, nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
				split.Event(_event).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
				split.OnExit(() => _caller.StateOnExitCall(1));
			});

			statechart.Run();
			statechart.Trigger(_event);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: LeaveState.Validate rejects a leave state with no transition at all — the null-conditional half of
		// its guard; the sibling below covers the transition-without-target half.
		// RCR: LeaveState.cs Validate — change the guard to `LeaveTransition != null && LeaveTransition.TargetState
		// == null` → RED (guard no longer fires; the layer check below dereferences null, so the thrown type is not
		// InvalidOperationException). Verified isolated: the sibling below stays green.
		public void LeaveState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var leave = factory.Leave("Leave");
				var final = SetupSimpleFlow(factory, leave);
			}));
		}

		[Test]
		// ADMIT: LeaveState.Validate rejects a transition declared without a Target — the `.TargetState == null`
		// half of the same guard the sibling above exercises.
		// RCR: LeaveState.cs Validate — change the guard to `LeaveTransition == null` → RED (guard no longer fires
		// for a targetless transition, so the layer check throws the wrong type). Verified isolated: the sibling
		// above stays green.
		public void LeaveState_MissingTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var leave = factory.Leave("Leave");
				var final = SetupSimpleFlow(factory, leave);

				leave.Transition().OnTransition(() => _caller.OnTransitionCall(1));
			}));
		}

		[Test]
		// ADMIT: LeaveState.Transition() rejects a second call, so a leave state cannot end up with an ambiguous
		// pair of exits where the later silently replaces the first.
		// RCR: LeaveState.cs Transition — change `if (LeaveTransition != null)` to `if (false)` → RED (no
		// InvalidOperationException; the second transition just overwrites).
		public void LeaveState_MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var nest = factory.Nest("Nest");
				var final = SetupSimpleFlow(factory, nest);

				_nestedStateData.Setup = factory =>
				{
					var leave = SetupLeave(factory, final);

					leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				};

				nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			}));
		}

		[Test]
		// ADMIT: LeaveState.Validate requires the target to sit exactly one region layer ABOVE the leave state, so a
		// leave pointing at its own layer is rejected rather than looping inside the region it means to exit.
		// RCR: LeaveState.cs Validate — change the layer check's `RegionLayer - 1` to `RegionLayer` → RED (the
		// same-layer target now passes). Verified: leaves the wrong-layer sibling below green, which is what
		// separates the two halves of this guard.
		public void LeaveState_SameLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var leave = factory.Leave("Leave");
				var final = SetupSimpleFlow(factory, leave);

				leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			}));
		}

		[Test]
		// ADMIT: the same layer check also rejects a target further than one layer up — a leave nested two regions
		// deep may not jump straight to the outermost layer.
		// RCR: LeaveState.cs Validate — change the layer check's `RegionLayer - 1` to `RegionLayer - 2` → RED (the
		// two-layer jump now passes). Verified: leaves the same-layer sibling above green.
		public void LeaveState_WrongLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory1 =>
			{
				var nest1 = factory1.Nest("Nest");
				var final1 = SetupSimpleFlow(factory1, nest1);

				_nestedStateData.Setup = factory2 => SetupLeave(factory2, final1);

				nest1.Nest(factory3 =>
				{
					var nest2 = factory3.Nest("Nest");
					var final2 = SetupSimpleFlow(factory3, nest2);

					nest2.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final2);
				}).OnTransition(() => _caller.OnTransitionCall(2)).Target(final1);
			}));
		}

		#region Setups

		private IFinalState SetupSimpleFlow(IStateFactory factory, IState state)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));

			return final;
		}

		private ILeaveState SetupLeave(IStateFactory factory, IState leaveTarget)
		{
			var leave = factory.Leave("Leave");
			var final = SetupSimpleFlow(factory, leave);

			leave.OnEnter(() => _caller.StateOnEnterCall(0));
			leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(leaveTarget);

			return leave;
		}

		private void SetupNest(IStateFactory factory)
		{
			var nest = factory.Nest("Nest");
			var final = SetupSimpleFlow(factory, nest);

			_nestedStateData.Setup = factory => SetupLeave(factory, final);

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			nest.Event(_event).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));
		}

		private void SetupSplit(IStateFactory factory)
		{
			var split = factory.Split("Split");
			var final = SetupSimpleFlow(factory, split);

			_nestedStateData.Setup = factory => SetupLeave(factory, final);

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(_nestedStateData, _nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			split.Event(_event).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));
		}

		#endregion
	}
}