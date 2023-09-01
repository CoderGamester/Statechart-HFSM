using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartChoiceTest
	{
		private IMockCaller _caller;
		private bool _condition1;
		private bool _condition2;
		
		[SetUp]
		public void Init()
		{
			_condition1 = _condition2 = false;
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupChoiceState);

			_condition1 = false;
			_condition2 = true;

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void ChoiceState_MultipleTrueConditions_PicksFirstTransition()
		{
			var statechart = new Statechart(SetupChoiceState);

			_condition1 = true;
			_condition2 = true;

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnEnterCall(2);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void ChoiceState_MissingTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var choice = factory.Choice("Choice");
				var final = SetupSimpleFlow(factory, choice);
			}));
		}

		[Test]
		public void ChoiceState_MissingConditionTransition_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var choice = factory.Choice("Choice");
				var final = SetupSimpleFlow(factory, choice);

				choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			}));
		}

		[Test]
		public void ChoiceState_OnlyConditionTransition_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var choice = factory.Choice("Choice");
				var final = SetupSimpleFlow(factory, choice);

				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			}));
		}

		[Test]
		public void ChoiceState_WithoutTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var choice = factory.Choice("Choice");
				var final = SetupSimpleFlow(factory, choice);

				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1));
				choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2));
				choice.Transition().OnTransition(() => _caller.OnTransitionCall(3));
			}));
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var choice = factory.Choice("Choice");
				var final = SetupSimpleFlow(factory, choice);

				choice.OnEnter(() => _caller.StateOnEnterCall(1));
				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(choice);
				choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(choice);
				choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(choice);
				choice.OnExit(() => _caller.StateOnExitCall(1));
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

		private void SetupChoiceState(IStateFactory factory)
		{
			var choice = factory.Choice("Choice");
			var final = SetupSimpleFlow(factory, choice);

			choice.OnEnter(() => _caller.StateOnEnterCall(1));
			choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			choice.OnExit(() => _caller.StateOnExitCall(1));
		}
	}
}