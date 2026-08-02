using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartNestTest
	{		
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;
		private NestedStateData _nestedStateData;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_nestedStateData = new NestedStateData(SetupNestedFlow);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates only lets the nest complete once its inner chart has reached a
		// FinalState — any non-final inner state nulls the pending transition and keeps the nest parked.
		// RCR: SplitState.cs ProcessInnerStates — change the `is not FinalState` branch to `else if (false)` → RED
		// (the nest completes immediately). Broad by nature: reddens most of this fixture, since every test here
		// depends on the nest not completing early.
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Enter fans out the nest's OnEnter actions before the inner chart runs, so a nest whose
		// own transition has no Target still enters and drives its inner region.
		// RCR: SplitState.cs Enter — replace the `_onEnter` fan-out source with an empty list → RED
		// (StateOnEnterCall never received). Overlaps most of the fixture, which also asserts the entry hook.
		public void NestedState_WithoutTarget_Successful()
		{
			var statechart = new Statechart(factory =>
			{
				var nest = factory.Nest("Nest");
				var final = SetupSimpleFlow(factory, nest);

				nest.OnEnter(() => _caller.StateOnEnterCall(1));
				nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2));
				nest.OnExit(() => _caller.StateOnExitCall(1));
			});

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates drains each inner state to a standstill (`while (nextState != null)`)
		// before judging completion, so an inner event that chains several transitions lands on Final in one pass.
		// RCR: SplitState.cs ProcessInnerStates — change the inner drain loop to `while (false)` → RED (the inner
		// chart advances one step per outer trigger and never reaches Final).
		public void NestedState_InnerEventTrigger_CompleteSuccess()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: same inner-drain contract as InnerEventTrigger_CompleteSuccess, with ExecuteFinal off.
		// RCR: SplitState.cs ProcessInnerStates — inner drain loop to `while (false)` → RED. NOTE: no probed
		// mutation separates this from the CompleteSuccess sibling — the ExecuteFinal flag is unreachable on this
		// path because the inner state IS a FinalState by then, which the flag's own guard excludes. Suspected A5
		// duplicate pending a decision; not deleted without proof that no mutation distinguishes them.
		public void NestedState_InnerEventTrigger_DisableExecuteFinal_CompleteSuccess()
		{
			_nestedStateData.ExecuteFinal = false;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: same inner-drain contract, with ExecuteExit off.
		// RCR: SplitState.cs ProcessInnerStates — inner drain loop to `while (false)` → RED. NOTE: as above, no
		// probed mutation separates this from the CompleteSuccess sibling — flipping ExecuteExit in either direction
		// leaves all four InnerEventTrigger variants green. Suspected A5 duplicate pending a decision.
		public void NestedState_InnerEventTrigger_DisableExecuteExit_CompleteSuccess()
		{
			_nestedStateData.ExecuteExit = false;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: same inner-drain contract, with both ExecuteExit and ExecuteFinal off.
		// RCR: SplitState.cs ProcessInnerStates — inner drain loop to `while (false)` → RED. NOTE: as above,
		// indistinguishable from the CompleteSuccess sibling by every probed mutation. Suspected A5 duplicate.
		public void NestedState_InnerEventTrigger_DisableExecuteExitFinal_CompleteSuccess()
		{
			_nestedStateData.ExecuteFinal = false;
			_nestedStateData.ExecuteExit = false;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Enter rewinds every inner region to its InitialState, so re-entering a nest after Reset
		// replays the inner chart instead of resuming where it stopped.
		// RCR: SplitState.cs Enter — change `innerState.CurrenState = innerState.InitialState;` to keep the existing
		// state when set → RED. Verified ISOLATED: the only test in this fixture that re-enters a nest.
		public void NestedState_InnerEventTrigger_RunResetRun_CompleteSuccess()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(4).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(4).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(4).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit runs each inner region's Exit when ExecuteExit is set, so force-completing a nest
		// from the outside still tears the inner state down.
		// RCR: SplitState.cs Exit — change `if (innerState.ExecuteExit)` to `if (false)` → RED. Leaves the three
		// DisableExecuteExit siblings green, which is what separates the flag's two directions.
		public void NestedState_EventTrigger_ForceCompleteSuccess()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must HONOUR ExecuteFinal being off — a force-completed nest with the flag cleared
		// must not synthesise an inner FinalState entry.
		// RCR: SplitState.cs Exit — ignore the flag, `if (true && !(innerState.CurrenState is FinalState) ...)` →
		// RED (the inner final hook fires when the caller disabled it). Leaves the flag-on siblings green.
		public void NestedState_EventTrigger_DisableExecuteFinal_ForceCompleteSuccess()
		{
			_nestedStateData.ExecuteFinal = false;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must HONOUR ExecuteExit being off — a force-completed nest with the flag cleared
		// must leave the inner state's exit hook alone.
		// RCR: SplitState.cs Exit — ignore the flag, `if (true)` → RED (the inner exit hook fires when the caller
		// disabled it). Leaves the flag-on siblings green.
		public void NestedState_EventTrigger_DisableExecuteExit_ForceCompleteSuccess()
		{
			_nestedStateData.ExecuteExit = false;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: with both flags cleared, SplitState.Exit must skip the inner final hook as well as the inner exit.
		// RCR: SplitState.cs Exit — ignore the ExecuteFinal flag, `if (true && !(innerState.CurrenState is
		// FinalState) ...)` → RED. Leaves the flag-on siblings green.
		public void NestedState_EventTrigger_DisableExecuteExitFinal_ForceCompleteSuccess()
		{
			_nestedStateData.ExecuteFinal = false;
			_nestedStateData.ExecuteExit = false;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: the inner-drain contract holds per region, so a nest with several inner regions still lands each on
		// Final in one pass.
		// RCR: SplitState.cs ProcessInnerStates — inner drain loop to `while (false)` → RED (no region reaches
		// Final). Shares this mutation with the single-region siblings above.
		public void MultipleNestedStates_InnerEventTrigger_CompleteSuccess()
		{
			_nestedStateData.Setup = SetupLayer0;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				_nestedStateData.Setup = SetupNestedFlow;

				SetupNest(factory);
			}
		}

		[Test]
		// ADMIT: same multi-region inner-drain contract with both execute flags cleared.
		// RCR: SplitState.cs ProcessInnerStates — inner drain loop to `while (false)` → RED. NOTE: as with the
		// single-region variants, no probed mutation separates this from its flags-on sibling. Suspected A5.
		public void MultipleNestedStates_InnerEventTrigger_DisableExecuteExitFinal_CompleteSuccess()
		{
			_nestedStateData.ExecuteFinal = false;
			_nestedStateData.ExecuteExit = false;
			_nestedStateData.Setup = SetupLayer0;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				_nestedStateData.ExecuteFinal = false;
				_nestedStateData.ExecuteExit = false;
				_nestedStateData.Setup = SetupNestedFlow;

				SetupNest(factory);
			}
		}

		[Test]
		// ADMIT: SplitState.Exit synthesises the inner FinalState entry for every region that had not reached Final
		// when the nest was force-completed from outside.
		// RCR: SplitState.cs Exit — change `if (innerState.ExecuteFinal && ...)` to `if (false && ...)` → RED (the
		// unfinished regions never get their final hook). Leaves the DisableExecuteFinal siblings green.
		public void MultipleNestedStates_EventTrigger_ForceCompleteSuccess()
		{
			_nestedStateData.Setup = SetupLayer0;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				_nestedStateData.Setup = SetupNestedFlow;

				SetupNest(factory);
			}
		}

		[Test]
		// ADMIT: with both flags cleared, force-completing a multi-region nest must touch neither inner exits nor
		// inner final hooks.
		// RCR: SplitState.cs Exit — ignore ExecuteExit, `if (true)` → RED (inner exits fire when disabled). Leaves
		// the flag-on siblings green.
		public void MultipleNestedStates_EventTrigger__DisableExecuteExitFinal_ForceCompleteSuccess()
		{
			_nestedStateData.ExecuteFinal = false;
			_nestedStateData.ExecuteExit = false;
			_nestedStateData.Setup = SetupLayer0;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				_nestedStateData.ExecuteFinal = false;
				_nestedStateData.ExecuteExit = false;
				_nestedStateData.Setup = SetupNestedFlow;

				SetupNest(factory);
			}
		}

		[Test]
		// ADMIT: a nest declared with no inner setup is rejected at construction rather than running as an empty
		// region that can never complete.
		// RCR: none exists — an empty nest trips BOTH NestState.Validate's `_innerStatesData.Count != 1` and
		// SplitState.OnValidate's `_innerStatesData.Count == 0`. Disabling either leaves the other throwing (both
		// directions verified green). Double-covered, not single-line falsifiable.
		public void NestedState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var nest = factory.Nest("Nest");
				var final = SetupSimpleFlow(factory, nest);
			}));
		}

		[Test]
		// ADMIT: SplitState.OnValidate rejects a nest whose completion transition targets the nest itself, which
		// would re-enter the region forever.
		// RCR: SplitState.cs OnValidate — change `if (_transition.TargetState?.Id == Id)` to `if (false)` → RED (no
		// InvalidOperationException). Verified ISOLATED.
		public void NestedState_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var nest = factory.Nest("Nest");
				var final = SetupSimpleFlow(factory, nest);

				nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(3)).Target(nest);
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

		private void SetupNest(IStateFactory factory)
		{
			var nest = factory.Nest("Nest");
			var final = SetupSimpleFlow(factory, nest);

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));
		}

		private void SetupNestedFlow(IStateFactory factory)
		{
			var state = factory.State("State");
			var final = SetupSimpleFlow(factory, state);

			state.OnEnter(() => _caller.StateOnEnterCall(0));
			state.Event(_event1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			state.OnExit(() => _caller.StateOnExitCall(0));
		}

		#endregion
	}
}