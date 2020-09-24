using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc cref="ITaskWaitState"/>
	internal class TaskWaitState : StateInternal, ITaskWaitState
	{
		private ITransitionInternal _transition;
		private Func<Task> _taskAwaitAction;
		private bool _triggered;
		private bool _completed;
		private uint _executionCount;

		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly Dictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public TaskWaitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_triggered = false;
			_completed = false;
			_executionCount = 0;
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_triggered = false;
			_completed = false;
			
			for(int i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			for(int i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_taskAwaitAction == null)
			{
				throw new MissingMethodException($"The state {Name} doesn't have a task await action");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}
#endif
		}

		/// <inheritdoc />
		public void OnEnter(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null OnEnter action");
			}

			_onEnter.Add(action);
		}

		/// <inheritdoc />
		public void OnExit(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null OnExit action");
			}

			_onExit.Add(action);
		}

		/// <inheritdoc />
		public ITransition Event(IStatechartEvent statechartEvent)
		{
			if (statechartEvent == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null event");
			}

			var transition = new Transition();

			_events.Add(statechartEvent, transition);

			return transition;
		}

		/// <inheritdoc />
		public ITransition WaitingFor(Func<Task> taskAwaitAction)
		{
			_taskAwaitAction = taskAwaitAction ?? throw new NullReferenceException($"The state {Name} cannot have a null wait action");
			_transition = new Transition();

			return _transition;
		}
		
		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out var transition))
			{
				return transition;
			}

			if (!_triggered)
			{
				InnerTaskAwait(_taskAwaitAction);
				_triggered = true;
			}

			return _completed ? _transition : null;
		}

		private async void InnerTaskAwait(Func<Task> taskAwaitAction)
		{
			var currentExecution = _executionCount;

			_executionCount++;
			
			await taskAwaitAction();

			_completed = true;

			// Checks if the state didn't exited from an outsource trigger (Nested State) before the Task was completed
			if (_executionCount - 1 == currentExecution)
			{
				_stateFactory.Data.StateChartMoveNextCall(null);
			}
		}
	}
}
