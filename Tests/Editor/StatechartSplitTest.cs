using System;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartSplitTest
	{
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;
		private NestedStateData[] _nestedStateData;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_nestedStateData = new NestedStateData[]
			{
				 new NestedStateData(SetupNestedFlow),
				 new NestedStateData(SetupNestedFlow)
			};
		}

		[Test]
		public void SimpleTest()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_WithoutTarget_Successful()
		{
			var statechart = new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);

				split.OnEnter(() => _caller.StateOnEnterCall(1));
				split.Split(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2));
				split.OnExit(() => _caller.StateOnExitCall(1));
			});

			statechart.Run();

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_InnerEventTrigger_CompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_InnerEventTrigger_HalfFinalized_OnHold()
		{
			_nestedStateData[0].Setup = factory =>
			{
				var state = factory.State("State");
				var final = SetupSimpleFlow(factory, state);

				state.OnEnter(() => _caller.StateOnEnterCall(0));
				state.OnExit(() => _caller.StateOnExitCall(0));
			};

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_InnerEventTrigger_DisableExecuteFinal_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_InnerEventTrigger_DisableExecuteExit_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_InnerEventTrigger_DisableExecuteExitFinal_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(3).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_InnerEventTrigger_RunResetRun_CompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(6).OnTransitionCall(0);
			_caller.Received(4).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(6).InitialOnExitCall(0);
			_caller.Received(4).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(4).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(6).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_EventTrigger_ForceCompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_EventTrigger_DisableExecuteExit_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_EventTrigger_DisableExecuteFinal_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void SplitedState_EventTrigger_DisableExecuteExitFinal_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
		}

		[Test]
		public void MultipleSplitedStates_InnerEventTrigger_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(5).OnTransitionCall(0);
			_caller.Received(3).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(3).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(5).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		public void MultipleSplitedStates_InnerEventTrigger_DisableExecuteExitFinal_CompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;
				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(5).OnTransitionCall(0);
			_caller.Received(3).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(3).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(5).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.ExecuteFinal = false;
					data.ExecuteExit = false;
					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		public void MultipleSplitedStates_EventTrigger_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(5).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(3).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(5).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		public void MultipleSplitedStates_EventTrigger__DisableExecuteExitFinal_ForceCompleteSuccess()
		{
			for (int i = 0; i < _nestedStateData.Length; i++)
			{
				var data = _nestedStateData[i];

				data.ExecuteFinal = false;
				data.ExecuteExit = false;
				data.Setup = SetupLayer0;

				_nestedStateData[i] = data;
			}

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(5).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(5).InitialOnExitCall(0);
			_caller.Received(3).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);

			void SetupLayer0(IStateFactory factory)
			{
				for (int i = 0; i < _nestedStateData.Length; i++)
				{
					var data = _nestedStateData[i];

					data.ExecuteFinal = false;
					data.ExecuteExit = false;
					data.Setup = SetupNestedFlow;

					_nestedStateData[i] = data;
				}

				SetupSplit(factory);
			}
		}

		[Test]
		public void SplitState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);
			}));
		}

		[Test]
		public void SplitState_SingleConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);

				split.Split(SetupNestedFlow).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			}));
		}

		[Test]
		public void SplitState_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var split = factory.Split("Split");
				var final = SetupSimpleFlow(factory, split);

				split.Split(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(split);
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

		private void SetupSplit(IStateFactory factory)
		{
			var split = factory.Split("Split");
			var final = SetupSimpleFlow(factory, split);

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));
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