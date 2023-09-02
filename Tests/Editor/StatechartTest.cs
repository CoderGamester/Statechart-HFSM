using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartTest
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
			var statechart = new Statechart(SetupSimpleFlow);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void InitialState_MissingTransition_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");
			}));
		}

		[Test]
		public void InitialState_TransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0));
			}));
		}

		[Test]
		public void InitialState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("Final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(initial);
			}));
		}

		[Test]
		public void InitialState_MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("Final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
				initial.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			}));
		}

		[Test]
		public void NoInitialState_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var final = factory.Final("final");
			}));
		}

		[Test]
		public void MultipleInitialStates_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				SetupSimpleFlow(factory);

				var initial1 = factory.Initial("Initial1");
			}));
		}

		[Test]
		public void MultipleFinalState_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				SetupSimpleFlow(factory);

				var final2 = factory.Final("final2");
			}));
		}
		
		private void SetupSimpleFlow(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}