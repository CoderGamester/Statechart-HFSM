using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <inheritdoc cref="ITaskWaitState"/>
	internal class TaskWaitState : StateInternal, ITaskWaitState
	{
		private ITransitionInternal _transition;
		private Func<Task> _taskAwaitAction;
		private IStatechartEvent _eventQueued;
		private bool _triggered;

		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();

		/// <summary>
		/// Requests the completion state of this task await state
		/// </summary>
		public bool Completed { get; private set; }

		public TaskWaitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_triggered = false;
			Completed = false;
		}

		/// <summary>
		/// If this task is still being executed, then queue the first command to be executed after the task is completed
		/// </summary>
		public void EnqueuEvent(IStatechartEvent statechartEvent)
		{
			_eventQueued ??= statechartEvent;
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_eventQueued = null;
			_triggered = false;
			Completed = false;

			for (int i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			Completed = true;
			
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
				throw new InvalidOperationException($"The state {Name} doesn't have a task await action");
			}

			if (_transition?.TargetState == null)
			{
				throw new InvalidOperationException($"The state {Name} is not pointing to any state");
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
		public ITransition WaitingFor(Func<Task> taskAwaitAction)
		{
			_taskAwaitAction = taskAwaitAction ?? throw new NullReferenceException($"The state {Name} cannot have a null wait action");
			_transition = new Transition();

			return _transition;
		}
		
		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (!_triggered)
			{
				_triggered = true;
				_ = InnerTaskAwait(statechartEvent?.Name);
			}

			return Completed ? _transition : null;
		}

		private async Task InnerTaskAwait(string eventName)
		{
			try
			{
				if (IsStateLogsEnabled)
				{
					Debug.Log($"TaskWait - '{eventName}' : '{_taskAwaitAction.Target}.{_taskAwaitAction.Method.Name}()' => '{Name}'");
				}

				//await Task.Yield();
				await _taskAwaitAction();

				Completed = true;

				_stateFactory.Data.StateChartMoveNextCall(_eventQueued);
			}
			catch (Exception e)
			{
				var finalMessage = "";
#if UNITY_EDITOR || DEBUG
				finalMessage = $"\nStackTrace log of '{Name}' state creation bellow.\n{CreationStackTrace}";
#endif

				Debug.LogError($"Exception in the state '{Name}', when calling the task wait action" +
					$"'{_taskAwaitAction.Target}.{_taskAwaitAction.Method.Name}()'.\n" +
					$"-->> Check the exception log after this one for more details <<-- {finalMessage}");
				Debug.LogException(e);
			}
		}
	}
}
