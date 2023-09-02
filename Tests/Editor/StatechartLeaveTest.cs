using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartLeaveTest
	{		
		private readonly IStatechartEvent _event = new StatechartEvent("Event");

		private IMockCaller _caller;
		private NestedStateData _nestedStateData;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_nestedStateData = new NestedStateData(SetupNest);
		}

		[Test]
		public void SimpleNestTest()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void SimpleSplitTest()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitState_OnlyLeaveInnerStates_LeaveFirstState()
		{
			var statechart = new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);
				var nestedStateData = _nestedStateData = new NestedStateData(factory => SetupLeave(factory, final));

				split.OnEnter(() => _caller.StateOnEnterCall(1));
				split.Split(_nestedStateData, nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
				split.Event(_event).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
				split.OnExit(() => _caller.StateOnExitCall(1));
			});

			statechart.Run();
			statechart.Trigger(_event);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void LeaveState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var leave = factory.Leave("Leave");
				var final = SetupSimpleFlow(factory, leave);
			}));
		}

		[Test]
		public void LeaveState_MissingTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var leave = factory.Leave("Leave");
				var final = SetupSimpleFlow(factory, leave);

				leave.Transition().OnTransition(() => _caller.OnTransitionCall(1));
			}));
		}

		[Test]
		public void LeaveState_MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var nest = factory.Nest("Nest");
				var final = SetupSimpleFlow(factory, nest);

				_nestedStateData.Setup = factory =>
				{
					var leave = SetupLeave(factory, final);

					leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				};

				nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			}));
		}

		[Test]
		public void LeaveState_SameLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var leave = factory.Leave("Leave");
				var final = SetupSimpleFlow(factory, leave);

				leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			}));
		}

		[Test]
		public void LeaveState_WrongLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory1 =>
			{
				var nest1 = factory1.Nest("Nest");
				var final1 = SetupSimpleFlow(factory1, nest1);

				_nestedStateData.Setup = factory2 => SetupLeave(factory2, final1);

				nest1.Nest(factory3 =>
				{
					var nest2 = factory3.Nest("Nest");
					var final2 = SetupSimpleFlow(factory3, nest2);

					nest2.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final2);
				}).OnTransition(() => _caller.OnTransitionCall(2)).Target(final1);
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

		private ILeaveState SetupLeave(IStateFactory factory, IState leaveTarget)
		{
			var leave = factory.Leave("Leave");
			var final = SetupSimpleFlow(factory, leave);

			leave.OnEnter(() => _caller.StateOnEnterCall(0));
			leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(leaveTarget);

			return leave;
		}

		private void SetupNest(IStateFactory factory)
		{
			var nest = factory.Nest("Nest");
			var final = SetupSimpleFlow(factory, nest);

			_nestedStateData.Setup = factory => SetupLeave(factory, final);

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			nest.Event(_event).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));
		}

		private void SetupSplit(IStateFactory factory)
		{
			var split = factory.Split("Split");
			var final = SetupSimpleFlow(factory, split);

			_nestedStateData.Setup = factory => SetupLeave(factory, final);

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(_nestedStateData, _nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			split.Event(_event).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));
		}

		#endregion
	}
}