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
		public void TransitionState_TransitionWithoutTransition_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var transition = factory.Transition("Transition");
				var final = SetupSimpleFlow(factory, transition);
			}));
		}

		[Test]
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