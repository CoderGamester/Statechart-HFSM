using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartNest_IntegrationTest
	{
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		/*[Test]
		public void BasicSetup()
		{
			var nestedStateData = new NestedStateData(SetupSimple, true, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

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
		public async Task NestState_OuterEventTriggerAndActivityComplete_NoTransition()
		{
			IWaitActivity activity = null;

			var statechart = new Statechart(InternalSetupNest);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.DidNotReceive().OnTransitionCall(5);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			statechart.Trigger(_event2);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().OnTransitionCall(5);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(1).OnTransitionCall(5);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void InternalSetupNest(IStateFactory factory)
			{
				SetupNest(factory, _event2, new NestedStateData(InnerSetupWaitState, true, false));
			}

			void InnerSetupWaitState(IStateFactory factory)
			{
				SetupWaitState(factory, waitActivity => activity = waitActivity);
			}
		}

		[UnityTest]
		public IEnumerator NestState_OuterEventTriggerAndTaskComplete_NoTransition()
		{
			var statechart = new Statechart(InternalSetupNest);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.DidNotReceive().OnTransitionCall(5);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			statechart.Trigger(_event2);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().OnTransitionCall(5);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
			
			_blocker = false;
			
			yield return YieldCoroutine();
				
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(1).OnTransitionCall(5);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void InternalSetupNest(IStateFactory factory)
			{
				SetupNest(factory, _event2, new NestedStateData(InnerSetupTaskWaitState, true, false));
			}

			void InnerSetupTaskWaitState(IStateFactory factory)
			{
				SetupTaskWaitState(factory, TaskWaitAction);
			}
		}*/
	}
}
