using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartTransitionTest
	{
		private IMockCaller _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		// ADMIT: TransitionState.Enter fans out its OnEnter actions as the chart passes through, so a
		// pass-through state still runs its entry hook on the way to the next state.
		// RCR: TransitionState.cs Enter — change the fan-out loop bound to `i < 0` → RED
		// (_caller.StateOnEnterCall(0) is never received).
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupTransitionFlow);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: TransitionState.Validate rejects a transition declared without a Target — the
		// `.TargetState == null` half of its guard; the sibling below covers the no-transition-at-all half.
		// RCR: TransitionState.cs Validate — change the guard to `_transition == null` (no longer inspects
		// TargetState) → RED (no InvalidOperationException). The sibling below stays green under this edit.
		public void TransitionState_TransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var transition = factory.Transition("Transition");
				var final = SetupSimpleFlow(factory, transition);

				transition.Transition().OnTransition(() => _caller.OnTransitionCall(1));
			}));
		}

		[Test]
		// ADMIT: TransitionState.Validate rejects a transition state with no outgoing transition at all — the
		// null-conditional half of the same guard the sibling above exercises. Such a state would strand the
		// chart with nowhere to advance to.
		// RCR: TransitionState.cs Validate — change the guard to `_transition != null &&
		// _transition.TargetState == null` → RED (no InvalidOperationException). Sibling above stays green.
		public void TransitionState_TransitionWithoutTransition_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var transition = factory.Transition("Transition");
				var final = SetupSimpleFlow(factory, transition);
			}));
		}

		[Test]
		// ADMIT: TransitionState.Validate rejects a transition state targeting itself, which would spin the
		// chart on entry rather than advancing.
		// RCR: TransitionState.cs Validate — change `if (_transition.TargetState.Id == Id)` to `if (false)` →
		// RED (no InvalidOperationException).
		public void TransitionState_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var transition = factory.Transition("Transition");
				var final = SetupSimpleFlow(factory, transition);

				transition.OnEnter(() => _caller.StateOnEnterCall(0));
				transition.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(transition);
				transition.OnExit(() => _caller.StateOnExitCall(0));
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

		private void SetupTransitionFlow(IStateFactory factory)
		{
			var transition = factory.Transition("Transition");
			var final = SetupSimpleFlow(factory, transition);

			transition.OnEnter(() => _caller.StateOnEnterCall(0));
			transition.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			transition.OnExit(() => _caller.StateOnExitCall(0));
		}
	}
}