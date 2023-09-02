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