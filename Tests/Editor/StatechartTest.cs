using System;
using GameLovers.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartTest
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

		private IMockCaller _caller;
		private bool _condition1;
		private bool _condition2;
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");
		
		[SetUp]
		public void Init()
		{
			_condition1 = _condition2 = false;
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void Statechart_Run_Successfully()
		{
			var Statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			});

			Statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void InitialState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(initial);
			}));
		}

		[Test]
		public void SimpleState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var state = factory.State("State");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				state.OnEnter(() => _caller.StateOnEnterCall(1));
				state.Event(_event1).OnTransition(() => _caller.OnTransitionCall(1)).Target(state);
				state.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void WaitingState_StateTransitionsLoop_ThrowsException()
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

		[Test]
		public void ChoiceState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var choice = factory.Choice("Choice");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				choice.OnEnter(() => _caller.StateOnEnterCall(1));
				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(choice);
				choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(choice);
				choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(choice);
				choice.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void NestState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var nest = factory.Nest("Nest");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(nest);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				nest.OnEnter(() => _caller.StateOnEnterCall(1));
				nest.Nest(SetupSimpleState).OnTransition(() => _caller.OnTransitionCall(4)).Target(nest);
				nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(5)).Target(nest);
				nest.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void SplitState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var split = factory.Split("Nest");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(split);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				split.OnEnter(() => _caller.StateOnEnterCall(1));
				split.Split(SetupSimpleState, SetupSimpleState).OnTransition(() => _caller.OnTransitionCall(4)).Target(split);
				split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(5)).Target(split);
				split.OnExit(() => _caller.StateOnExitCall(1));
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
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void ChoiceState_MissingTransition_ThrowsException()
		{
			Assert.Throws<MissingMethodException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var choice = factory.Choice("Choice");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				choice.OnEnter(() => _caller.StateOnEnterCall(1));
				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
				choice.OnExit(() => _caller.StateOnExitCall(1));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void LeaveState_SameLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var leave = factory.Leave("Leave");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				leave.OnEnter(() => _caller.StateOnEnterCall(1));
				leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(initial);
			}));
		}

		[Test]
		public void LeaveState_WrongLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var state = factory.State("State");

				SetupNest(factory, nestFactory => SetupNest(nestFactory, doubleNestFactory => SetupLeave(doubleNestFactory, state)));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}));
		}

		[Test]
		public void LeaveState_MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var state = factory.Choice("State");

				SetupNest(factory, nestFactory =>
				{
					var initial = factory.Initial("Initial");
					var leave = factory.Leave("Leave");

					initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
					initial.OnExit(() => _caller.InitialOnExitCall(0));

					leave.OnEnter(() => _caller.StateOnEnterCall(1));
					leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(state);
					leave.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(state);
				});

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}));
		}

		[Test]
		public void NoInitialState_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var final = factory.Final("final");

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void MultipleInitialStates_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial1 = factory.Initial("Initial1");
				var initial2 = factory.Initial("Initial2");

				initial1.OnExit(() => _caller.InitialOnExitCall(1));
				initial2.OnExit(() => _caller.InitialOnExitCall(2));
			}));
		}

		[Test]
		public void MultipleFinalState_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final1 = factory.Final("final1");
				var final2 = factory.Final("final2");

				initial.OnExit(() => _caller.InitialOnExitCall(0));

				final1.OnEnter(() => _caller.FinalOnEnterCall(1));
				final2.OnEnter(() => _caller.FinalOnEnterCall(2));
			}));
		}

		[Test]
		public void StatechartSimpleState_Trigger_Successfully()
		{
			var Statechart = new Statechart(SetupSimpleState);

			Statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			Statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void StatechartSimpleState_TriggeOutsideEvent_DoesNothing()
		{
			var Statechart = new Statechart(SetupSimpleState);

			Statechart.Run();
			Statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnEnterCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		public void StatechartSimpleState_PauseTrigger_DoesNothing()
		{
			var Statechart = new Statechart(SetupSimpleState);

			Statechart.Run();
			Statechart.Pause();
			Statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnEnterCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		public void StatechartSimpleState_PauseRun_ResumesStatechart()
		{
			var Statechart = new Statechart(SetupSimpleState);

			Statechart.Run();
			Statechart.Pause();
			Statechart.Run();
			Statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void StatechartSimpleState_ResetRunTrigger_Successfully()
		{
			var Statechart = new Statechart(SetupSimpleState);

			Statechart.Run();
			Statechart.Reset();
			Statechart.Run();
			Statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void StatechartChoiceState()
		{
			var Statechart = new Statechart(SetupChoiceState);

			_condition1 = false;
			_condition2 = true;

			Statechart.Run();

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
		public void StatechartChoiceState_MultipleTrueConditions_PicksFirstTransition()
		{
			var Statechart = new Statechart(SetupChoiceState);

			_condition1 = true;
			_condition2 = true;

			Statechart.Run();

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
		public void StatechartNest_InnerEvent()
		{
			var Statechart = new Statechart(factory => SetupNest(factory, SetupSimpleState));

			Statechart.Run();
			Statechart.Trigger(_event1);

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
		public void StatechartNest_OutsideEvent()
		{
			var Statechart = new Statechart(factory => SetupNest(factory, SetupSimpleState));

			Statechart.Run();
			Statechart.Trigger(_event2);

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
		public void StatechartNest_RunResetRun_Successfully()
		{
			var Statechart = new Statechart(factory => SetupNest(factory, SetupSimpleState));

			Statechart.Run();
			Statechart.Trigger(_event1);
			Statechart.Reset();
			Statechart.Run();
			Statechart.Trigger(_event1);

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
		public void StatechartSplit_InnerEvent()
		{
			var Statechart = new Statechart(factory => SetupSplit(factory, SetupSimpleState, SetupSimpleState));

			Statechart.Run();
			Statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);
		}

		[Test]
		public void StatechartSplit_OutsideEvent()
		{
			var Statechart = new Statechart(factory => SetupSplit(factory, SetupSimpleState, SetupSimpleState));

			Statechart.Run();
			Statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);
		}

		[Test]
		public void StatechartSplit_RunResetRun_Successfully()
		{
			var Statechart = new Statechart(factory => SetupSplit(factory, SetupSimpleState, SetupSimpleState));

			Statechart.Run();
			Statechart.Trigger(_event1);
			Statechart.Reset();
			Statechart.Run();
			Statechart.Trigger(_event1);

			_caller.Received(4).OnTransitionCall(0);
			_caller.Received(4).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.Received(2).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(4).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(4).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(4).StateOnExitCall(1);
			_caller.Received(4).FinalOnEnterCall(0);
			_caller.Received(2).FinalOnEnterCall(1);
		}

		[Test]
		public void StatechartSplit_HalfFinal_NotFinalized()
		{
			var Statechart = new Statechart(factory => SetupSplit(factory, SetupSimpleState, SetupChoiceState));

			_condition1 = false;
			_condition2 = false;

			Statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);
		}

		[Test]
		public void StatechartWaitState_WaitActivityComplete_Successfully()
		{
			IWaitActivity activity = null;

			var Statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			Statechart.Run();

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
		public void StatechartWaitState_EventTrigger_Successfully()
		{
			var Statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => { }));

			Statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			Statechart.Trigger(_event1);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void StatechartWaitState_SplitComplete_Successfully()
		{
			IWaitActivity activity = null;
			IWaitActivity activitySplit = null;

			var Statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			Statechart.Run();

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
		public void StatechartLeave_WithSetupNest_Successfully()
		{
			var Statechart = new Statechart(factory =>
			{
				var state = factory.State("State");

				SetupNest(factory, nestFactory => SetupLeave(nestFactory, state));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			});

			Statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnEnterCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(1);
		}

		[Test]
		public void StatechartLeave_WithSetupSplit_Successfully()
		{
			var Statechart = new Statechart(factory =>
			{
				var state = factory.State("State");

				SetupSplit(factory, SetupChoiceState, nestFactory => SetupLeave(nestFactory, state));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			});

			_condition1 = false;
			_condition2 = false;

			Statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnEnterCall(2);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);
		}

		#region Setups

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

		private void SetupSimpleState(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var state = factory.State("State");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			state.OnEnter(() => _caller.StateOnEnterCall(1));
			state.Event(_event1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			state.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupChoiceState(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var choice = factory.Choice("Choice");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			choice.OnEnter(() => _caller.StateOnEnterCall(1));
			choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			choice.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupNest(IStateFactory factory, Action<IStateFactory> nestSetup)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(0));
			nest.Nest(nestSetup).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		private void SetupSplit(IStateFactory factory, Action<IStateFactory> setup1, Action<IStateFactory> setup2)
		{
			var initial = factory.Initial("Initial");
			var split = factory.Split("Split");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(split);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			split.OnEnter(() => _caller.StateOnEnterCall(0));
			split.Split(setup1, setup2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		private void SetupLeave(IStateFactory factory, IState leaveTarget)
		{
			var initial = factory.Initial("Initial");
			var leave = factory.Leave("Leave");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			leave.OnEnter(() => _caller.StateOnEnterCall(1));
			leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(leaveTarget);

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		#endregion
	}
}