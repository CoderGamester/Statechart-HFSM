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
		// ADMIT: ChoiceState.OnTrigger returns the first transition whose CheckCondition() is TRUE, so with
		// condition1 false and condition2 true the chart takes the second branch, not the first.
		// RCR: ChoiceState.cs OnTrigger — invert `if (_transitions[i].CheckCondition())` → RED
		// (OnTransitionCall(1) is received and OnTransitionCall(2) is not).
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
		// ADMIT: ChoiceState.OnTrigger scans _transitions in declaration order and returns on the FIRST true
		// condition, so two simultaneously-true conditions resolve deterministically to the earlier one.
		// RCR: ChoiceState.cs OnTrigger — reverse the scan to `for (var i = _transitions.Count - 1; i >= 0;
		// i--)` → RED (the later branch wins). SimpleTest stays green: only one condition is true there.
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
		// ADMIT: ChoiceState.Validate rejects a choice state with NO transitions at all.
		// RCR: no single-line mutation exists — the empty case trips BOTH independent guards
		// (!hasTransitionWithCondition and noTransitionConditionCount == 0), so disabling either one
		// leaves the other still throwing. Verified: narrowing the first to `&& _transitions.Count > 0`
		// left this test green. Double-covered belt-and-braces, not single-line falsifiable.
		public void ChoiceState_MissingTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var choice = factory.Choice("Choice");
				var final = SetupSimpleFlow(factory, choice);
			}));
		}

		[Test]
		// ADMIT: ChoiceState.Validate also rejects a choice state whose only transition carries no condition —
		// that is a TransitionState, not a choice — via the same !hasTransitionWithCondition guard.
		// RCR: ChoiceState.cs Validate — narrow the guard to `!hasTransitionWithCondition &&
		// _transitions.Count == 0` → RED (the one-unconditional-transition case no longer throws). The
		// sibling ChoiceState_MissingTransitions_ThrowsException stays green under this edit.
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
		// ADMIT: ChoiceState.Validate requires a fallback transition with no condition, so a choice whose
		// every condition evaluates false still has somewhere to go instead of silently stalling.
		// RCR: ChoiceState.cs Validate — change `if (noTransitionConditionCount == 0)` to `if (false)` → RED
		// (no InvalidOperationException).
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
		// ADMIT: ChoiceState.Validate rejects any transition left without a Target, naming the offending
		// transition index, rather than deferring to a null dereference at run time.
		// RCR: ChoiceState.cs Validate — change `if (_transitions[i].TargetState == null)` to `if (false)` →
		// RED (no InvalidOperationException; validation instead falls through to the next guard).
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
		// ADMIT: ChoiceState.Validate rejects a transition targeting its own choice state, which would
		// re-evaluate the same conditions forever.
		// RCR: ChoiceState.cs Validate — change `if (_transitions[i].TargetState.Id == Id)` to `if (false)` →
		// RED (no InvalidOperationException).
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