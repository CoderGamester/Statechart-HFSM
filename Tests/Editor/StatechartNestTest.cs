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
		public void NestedState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var nest = factory.Nest("Nest");
				var final = SetupSimpleFlow(factory, nest);
			}));
		}

		[Test]
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