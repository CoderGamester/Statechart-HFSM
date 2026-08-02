using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartStateTest
	{
		private IMockCaller _caller;
		private readonly IStatechartEvent _event = new StatechartEvent("Event");
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		// ADMIT: SimpleState.Exit fans out its OnExit actions when an event finally moves the state on, so a
		// waiting state still runs its exit hook rather than being abandoned in place.
		// RCR: SimpleState.cs Exit — change the fan-out loop bound to `i < 0` → RED (StateOnExitCall(1) is
		// never received after Trigger). Also reddens the Pause/Reset siblings, which assert the same hook.
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupStateFlow);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_caller.Received().OnTransitionCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: StateInternal.Trigger runs a targetless event transition's action but does NOT exit the state
		// — the `if (nextState == null)` early return fires TriggerTransition and returns without TriggerExit,
		// so the chart stays put instead of falling out of the state with nowhere to go.
		// RCR: StateInternal.cs Trigger — change `if (nextState == null)` to `if (false)` → RED (execution
		// falls through to TriggerExit, so DidNotReceive().StateOnExitCall(1) fails).
		public void State_TransitionWithoutTarget_Succeeds()
		{
			var statechart = new Statechart(factory =>
			{
				var state = factory.State("State");
				var final = SetupSimpleFlow(factory, state);

				state.OnEnter(() => _caller.StateOnEnterCall(1));
				state.Event(_event).OnTransition(() => _caller.OnTransitionCall(1));
				state.OnExit(() => _caller.StateOnExitCall(1));
			});

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: triggering an event the state never registered leaves the chart untouched.
		// RCR: no single-line mutation found. SimpleState.OnTrigger's miss path is a plain Dictionary
		// TryGetValue returning false, and StatechartEvent's Equals/GetHashCode are Id-based, so making the
		// lookup hit would take coordinated edits to both members — not one line, and the behaviour being
		// pinned is the BCL's, not this package's (A3). Review candidate rather than trusted coverage.
		public void State_TriggerNotConfiguredEvent_NoEffect()
		{
			var statechart = new Statechart(SetupStateFlow);
			var event2 = new StatechartEvent("Event2");

			statechart.Run();
			statechart.Trigger(event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnEnterCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: Statechart.Run re-arms _isRunning, so a chart resumed after Pause processes triggers again
		// instead of staying inert (Trigger early-returns while !_isRunning).
		// RCR: Statechart.cs Run — delete `_isRunning = true;` → RED (after Pause the chart never resumes, so
		// OnTransitionCall(1) is never received). Also reddens siblings, which all Run() first.
		public void State_PauseRunStatechart_Success()
		{
			var statechart = new Statechart(SetupStateFlow);

			statechart.Run();
			statechart.Pause();
			statechart.Run();
			statechart.Trigger(_event);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: Statechart.Reset rewinds _currentState to the factory's initial state, so a Reset+Run replays
		// the flow from the top — that replay is what makes the Received(2) counts below correct.
		// RCR: Statechart.cs Reset — delete `_currentState = _stateFactory.InitialState;` → RED (no replay, so
		// Received(2).OnTransitionCall(0) sees only 1 call).
		public void State_ResetRunStatechart_Success()
		{
			var statechart = new Statechart(SetupStateFlow);

			statechart.Run();
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SimpleState.Validate rejects an event transition that targets its own state, which would
		// re-enter the state forever rather than advancing.
		// RCR: SimpleState.cs Validate — change `if (eventTransition.Value.TargetState?.Id == Id)` to
		// `if (false)` → RED (no InvalidOperationException).
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var state = factory.State("State");
				
				SetupSimpleFlow(factory, state);

				state.OnEnter(() => _caller.StateOnEnterCall(1));
				state.Event(_event).OnTransition(() => _caller.OnTransitionCall(1)).Target(state);
				state.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		private IFinalState SetupSimpleFlow(IStateFactory factory, IState state)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));

			return final;
		}

		private void SetupStateFlow(IStateFactory factory)
		{
			var state = factory.State("State");
			var final =	SetupSimpleFlow(factory, state);

			state.OnEnter(() => _caller.StateOnEnterCall(1));
			state.Event(_event).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			state.OnExit(() => _caller.StateOnExitCall(1));
		}
	}
}