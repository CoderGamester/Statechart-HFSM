using System;
using System.Threading.Tasks;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartWaitTest
	{
		private readonly IStatechartEvent _event = new StatechartEvent("Event");

		private IMockCaller _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public async Task SimpleTest()
		{
			IWaitActivity activity = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activity.Complete();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public async Task SplitActivity_CompleteBoth_Success()
		{
			IWaitActivity activity = null;
			IWaitActivity activitySplit = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activitySplit = activity.Split();
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			activitySplit.Complete();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public async Task SplitActivity_CompleteOnlyOneActivity_OnHold()
		{
			IWaitActivity activity = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activity.Split();
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		public void WaitState_EventTrigger_ForceCompleted()
		{
			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => { }));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void WaitState_EventTriggerWithoutTarget_OnlyEvokesOnTransition()
		{
			var statechart = new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.OnEnter(() => _caller.StateOnEnterCall(0));
				waiting.WaitingFor(activity => {}).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				waiting.Event(_event).OnTransition(() => _caller.OnTransitionCall(2));
				waiting.OnExit(() => _caller.StateOnExitCall(0));
			});

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().OnTransitionCall(2);
		}

		[Test]
		public void WaitState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);
			}));
		}

		[Test]
		public void WaitState_MissingTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.WaitingFor(waitingActivity => waitingActivity.Complete());
			}));
		}

		[Test]
		public void WaitState_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.WaitingFor(waitingActivity => waitingActivity.Complete()).OnTransition(() => _caller.OnTransitionCall(1)).Target(waiting);
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

		private void SetupWaitState(IStateFactory factory, Action<IWaitActivity> waitAction)
		{
			var waiting = factory.Wait("Wait");
			var final = SetupSimpleFlow(factory, waiting);

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(waitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.Event(_event).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(0));
		}
	}
}