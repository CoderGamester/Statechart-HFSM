using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartSplitTest
	{
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;
		private NestedStateData[] _nestedStateData;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_nestedStateData = new NestedStateData[]
			{
				 new NestedStateData(SetupNestedFlow),
				 new NestedStateData(SetupNestedFlow)
			};
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates must hold the split open while any parallel region is still
		// running, so Run() alone never fires the split's completion transition.
		// RCR: SplitState.cs ProcessInnerStates — `else if (innerState.CurrenState is not FinalState)` to
		// `else if (false)` → RED (the split completes on Run: OnTransitionCall(2) and FinalOnEnterCall(0)
		// fire). Broad by nature: reddens every sibling that expects the split to stay open.
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.OnValidate must accept a split transition with no target — unlike WaitState, a
		// split may fan out purely for its OnTransition side effect.
		// RCR: SplitState.cs OnValidate — `if (_transition.TargetState?.Id == Id)` to
		// `if (_transition.TargetState == null || _transition.TargetState.Id == Id)` → RED (construction
		// now throws InvalidOperationException). The self-loop sibling stays green under this edit.
		public void SplitedState_WithoutTarget_Successful()
		{
			var statechart = new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);

				split.OnEnter(() => _caller.StateOnEnterCall(1));
				split.Split(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2));
				split.OnExit(() => _caller.StateOnExitCall(1));
			});

			statechart.Run();

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates must fan the triggering event into every parallel region, so
		// both regions reach Final and the split's own transition fires exactly once.
		// RCR: SplitState.cs ProcessInnerStates — `CurrenState.Trigger(statechartEvent)` to
		// `CurrenState.Trigger(null)` → RED (OnTransitionCall(1) and (2) never received). Also reddens the
		// three flag-disabling siblings below, which take the same path.
		public void SplitedState_InnerEventTrigger_CompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates must require ALL regions to be Final — one finished region
		// out of two leaves the split on hold.
		// RCR: SplitState.cs ProcessInnerStates — `else if (innerState.CurrenState is not FinalState)` to
		// `else if (i > 0 && ...)`, so the first region no longer vetoes → RED (the split completes with
		// region 0 still running). SimpleTest stays green: there region 1 still vetoes.
		public void SplitedState_InnerEventTrigger_HalfFinalized_OnHold()
		{
			_nestedStateData[0].Setup = factory =>
			{
				var state = factory.State("State");
				var final = SetupSimpleFlow(factory, state);

				state.OnEnter(() => _caller.StateOnEnterCall(0));
				state.OnExit(() => _caller.StateOnExitCall(0));
			};

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates drives both regions to Final on the inner event even when
		// the caller disabled NestedStateData.ExecuteFinal.
		// RCR: SplitState.cs ProcessInnerStates — `CurrenState.Trigger(statechartEvent)` to `Trigger(null)`
		// → RED (no region completes). NOTE: nothing separates this from the CompleteSuccess sibling —
		// ExecuteFinal's guard in Exit also requires the region NOT be a FinalState, which it is here.
		// Suspected A5 duplicate.
		public void SplitedState_InnerEventTrigger_DisableExecuteFinal_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates drives both regions to Final on the inner event even when
		// the caller disabled NestedStateData.ExecuteExit.
		// RCR: SplitState.cs ProcessInnerStates — `CurrenState.Trigger(statechartEvent)` to `Trigger(null)`
		// → RED (no region completes). NOTE: nothing separates this from the CompleteSuccess sibling — on
		// this path ExecuteExit only calls FinalState.Exit(), a no-op. Suspected A5 duplicate.
		public void SplitedState_InnerEventTrigger_DisableExecuteExit_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates drives both regions to Final on the inner event with both
		// NestedStateData flags disabled.
		// RCR: SplitState.cs ProcessInnerStates — `CurrenState.Trigger(statechartEvent)` to `Trigger(null)`
		// → RED (no region completes). NOTE: both flags are unreachable on this path (see the two siblings
		// above), so nothing separates this from CompleteSuccess. Suspected A5 duplicate.
		public void SplitedState_InnerEventTrigger_DisableExecuteExitFinal_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Enter must re-anchor every region to its InitialState, or a re-entered split
		// resumes from the Final states left behind by the previous run.
		// RCR: SplitState.cs Enter — `innerState.CurrenState = innerState.InitialState;` to
		// `innerState.CurrenState ??= innerState.InitialState;` → RED (the second Run completes the split
		// at once; OnTransitionCall(0) received 4 times, not 6). Only test here that re-enters a split.
		public void SplitedState_InnerEventTrigger_RunResetRun_CompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(6).OnTransitionCall(0);
			_caller.Received(4).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(6).InitialOnExitCall(0);
			_caller.Received(4).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(4).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(6).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must run each unfinished region's own exit hook when the split is
		// force-completed through its event transition.
		// RCR: SplitState.cs Exit — `if (innerState.ExecuteExit)` to `if (false)` → RED (StateOnExitCall(0)
		// never received). Leaves the two DisableExecuteExit siblings green, which is what separates the
		// flag's directions; reddens the DisableExecuteFinal sibling, which expects the same hook.
		public void SplitedState_EventTrigger_ForceCompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must honour NestedStateData.ExecuteExit=false and skip the inner region's
		// exit hook while still finalising that region.
		// RCR: SplitState.cs Exit — `if (innerState.ExecuteExit)` to `if (true)` → RED (StateOnExitCall(0)
		// fires when the caller disabled it). Leaves the flag-on siblings green; also reddens the
		// DisableExecuteExitFinal sibling.
		public void SplitedState_EventTrigger_DisableExecuteExit_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must honour NestedStateData.ExecuteFinal=false and skip the inner region's
		// FinalState.Enter() when the split is force-completed.
		// RCR: SplitState.cs Exit — ignore the flag, `if (true && !(innerState.CurrenState is FinalState)
		// && ...)` → RED (FinalOnEnterCall(0) received 3 times, not 1). Leaves the flag-on siblings green;
		// also reddens the DisableExecuteExitFinal sibling.
		public void SplitedState_EventTrigger_DisableExecuteFinal_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must treat the two NestedStateData flags independently — turning
		// ExecuteFinal off must not resurrect the exit hook the caller also disabled.
		// RCR: SplitState.cs Exit — `if (innerState.ExecuteExit)` to
		// `if (innerState.ExecuteExit || !innerState.ExecuteFinal)` → RED (StateOnExitCall(0) fires with
		// both flags off). Green for every sibling that leaves one flag on; reddens the Multiple variant.
		public void SplitedState_EventTrigger_DisableExecuteExitFinal_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates must keep draining a region until it settles, or a region
		// whose first state is itself a Split is entered but never driven.
		// RCR: SplitState.cs ProcessInnerStates — `while (nextState != null)` to
		// `while (nextState != null && nextState is not SplitState)` → RED (the inner split never runs;
		// OnTransitionCall(0) received 3 times, not 5). Reddens the three nested-split siblings too.
		public void MultipleSplitedStates_InnerEventTrigger_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(5).OnTransitionCall(0);
			_caller.Received(3).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(3).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(5).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		// ADMIT: SplitState.ProcessInnerStates drives a nested split to completion on the inner event with
		// both NestedStateData flags disabled at both layers.
		// RCR: SplitState.cs ProcessInnerStates — `while (nextState != null)` to
		// `while (nextState != null && nextState is not SplitState)` → RED (the inner split never runs).
		// NOTE: the flags are unreachable on this path, so nothing separates this from the flags-on sibling
		// above. Suspected A5 duplicate.
		public void MultipleSplitedStates_InnerEventTrigger_DisableExecuteExitFinal_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;
				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(5).OnTransitionCall(0);
			_caller.Received(3).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(3).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(5).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.ExecuteFinal = false;
					data.ExecuteExit = false;
					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		// ADMIT: SplitState.Exit must cascade into a region whose current state is itself a SplitState, so
		// the inner split's own exit and its regions' exits run as well.
		// RCR: SplitState.cs Exit — `if (innerState.ExecuteExit)` to `if (innerState.ExecuteExit &&
		// !(innerState.CurrenState is SplitState))` → RED (StateOnExitCall(1) received once, not twice).
		// Only test that force-completes over an unfinished inner split.
		public void MultipleSplitedStates_EventTrigger_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(5).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(3).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(5).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		// ADMIT: SplitState.Exit must honour ExecuteExit=false even when the region holds an unfinished
		// nested SplitState — the inner split is abandoned, not exited.
		// RCR: SplitState.cs Exit — `if (innerState.ExecuteExit)` to
		// `if (innerState.ExecuteExit || innerState.CurrenState is SplitState)` → RED (the inner split
		// exits: StateOnExitCall(0) fires and StateOnExitCall(1) is received twice). Isolated.
		public void MultipleSplitedStates_EventTrigger__DisableExecuteExitFinal_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;
				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(5).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.ExecuteFinal = false;
					data.ExecuteExit = false;
					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		// ADMIT: SplitState.Validate must reject a split with no nested setup, which would otherwise
		// dereference a null _transition inside OnValidate.
		// RCR: none exists — an empty split trips BOTH SplitState.Validate's `_innerStatesData.Count < 2`
		// and OnValidate's `_innerStatesData.Count == 0`; disabling either leaves the other throwing the
		// same InvalidOperationException. Double-covered, not single-line falsifiable.
		public void SplitState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);
			}));
		}

		[Test]
		// ADMIT: SplitState.Validate must reject a split configured with a single region — one region is a
		// Nest, not a Split.
		// RCR: SplitState.cs Validate — `if (_innerStatesData.Count < 2)` to
		// `if (_innerStatesData.Count < 1)` → RED (a one-region split constructs cleanly). The empty-split
		// sibling stays green: it still throws from the same guard. NestState overrides Validate.
		public void SplitState_SingleConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);

				split.Split(SetupNestedFlow).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			}));
		}

		[Test]
		// ADMIT: SplitState.OnValidate must reject a split whose completion transition targets the split
		// itself, which would loop forever at runtime.
		// RCR: SplitState.cs OnValidate — `if (_transition.TargetState?.Id == Id)` to `if (false)` → RED
		// (no InvalidOperationException). Isolated.
		public void SplitState_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);

				split.Split(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(split);
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

		private void SetupSplit(IStateFactory factory)
		{
			var split = factory.Split("Split");
			var final = SetupSimpleFlow(factory, split);

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));
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