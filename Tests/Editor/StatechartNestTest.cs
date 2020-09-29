using System;
using GameLovers.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.Statechart.Tests
{
	[TestFixture]
	public class StatechartNestTest
	{
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller
		{
			void InitialOnExitCall(int id);
			void FinalOnEnterCall(int id);
			void StateOnEnterCall(int id);
			void StateOnExitCall(int id);
			void OnTransitionCall(int id);
		}
		
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void BasicSetup()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimple, true,false));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void BasicSetup_WithoutTarget()
		{
			var statechart = new StateMachine(factory => SetupNest_WithoutTarget(factory, _event2, SetupSimple));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);
		}

		[Test]
		public void InnerEventTrigger()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, true,false));

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void InnerEventTrigger_ExecuteFinal_SameResult()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, true,false));

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void InnerEventTrigger_NotExecuteExit_SameResult()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, false,false));

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, true,false));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger_ExecuteFinal()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, true,true));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger_NotExecuteExit()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, false,false));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger_NotExecuteExit_ExecuteFinal()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, false,true));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void NestedStates_InnerEventTrigger()
		{
			var statechart = new StateMachine(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(1).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.Received(2).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(1).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.Received(2).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupNest(factory, _event2, SetupLayer1, true,false);
			}

			void SetupLayer1(IStateFactory factory)
			{
				SetupNest(factory, _event2, SetupSimpleEventState, true,false);
			}
		}

		[Test]
		public void NestedStates_OuterEventLayer0()
		{
			var statechart = new StateMachine(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(1).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(1).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupNest(factory, _event2, SetupLayer1, true,false);
			}

			void SetupLayer1(IStateFactory factory)
			{
				SetupNest(factory, _event1, SetupSimpleEventState, true,false);
			}
		}

		[Test]
		public void InnerEventTrigger_RunResetRun()
		{
			var statechart = new StateMachine(factory => SetupNest(factory, _event2, SetupSimpleEventState, true,false));

			statechart.Run();
			statechart.Trigger(_event1);
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.Received(2).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(2).FinalOnEnterCall(1);
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new StateMachine(factory =>
			{
				var initial = factory.Initial("Initial");
				var nest = factory.Nest("Nest");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(nest);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				nest.OnEnter(() => _caller.StateOnEnterCall(1));
				nest.Nest(SetupSimpleEventState).OnTransition(() => _caller.OnTransitionCall(4)).Target(nest);
				nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(5)).Target(nest);
				nest.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		#region Setups
		
		private void SetupSimple(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupSimpleEventState(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var state = factory.State("State");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			state.OnEnter(() => _caller.StateOnEnterCall(0));
			state.Event(_event1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			state.OnExit(() => _caller.StateOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupNest(IStateFactory factory, IStatechartEvent eventTrigger, Action<IStateFactory> nestSetup,
		                       bool executeExit, bool executeFinal)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(nestSetup, executeExit, executeFinal).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		private void SetupNest_WithoutTarget(IStateFactory factory, IStatechartEvent eventTrigger, Action<IStateFactory> nestSetup)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(nestSetup, true, false).OnTransition(() => _caller.OnTransitionCall(3));
			nest.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(4));
			nest.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		#endregion
	}
}