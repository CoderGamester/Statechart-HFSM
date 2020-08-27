using System;
using GameLovers.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartWaitTest
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

		private IMockCaller _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void BasicSetup()
		{
			IWaitActivity activity = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			activity.Complete();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void EventTrigger()
		{
			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => { }));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event1);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void SplitActivity_CompleteBoth()
		{
			IWaitActivity activity = null;
			IWaitActivity activitySplit = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			activitySplit = activity.Split();
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			activitySplit.Complete();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void SplitActivity_CompleteOnlyOneActivity()
		{
			IWaitActivity activity = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			activity.Split();
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.Wait("Waiting");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				waiting.OnEnter(() => _caller.StateOnEnterCall(1));
				waiting.WaitingFor(waitingActivity => waitingActivity.Complete()).OnTransition(() => _caller.OnTransitionCall(1)).Target(waiting);
				waiting.Event(_event1).OnTransition(() => _caller.OnTransitionCall(2)).Target(waiting);
				waiting.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		private void SetupWaitState(IStateFactory factory, Action<IWaitActivity> waitAction)
		{
			var initial = factory.Initial("Initial");
			var waiting = factory.Wait("Wait");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			waiting.OnEnter(() => _caller.StateOnEnterCall(1));
			waiting.WaitingFor(waitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.Event(_event1).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}