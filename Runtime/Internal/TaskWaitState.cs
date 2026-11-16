using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <inheritdoc cref="ITaskWaitState"/>
	internal class TaskWaitState : StateInternal, ITaskWaitState
	{
		private ITransitionInternal _transition;
		private Func<CancellationToken, UniTask> _taskAwaitAction;
		private IStatechartEvent _eventQueued;
		private bool _triggered;
		private int _enterCount;

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
		public override StateKind Kind => StateKind.TaskWait;

		/// <inheritdoc />
		public override void Enter()
		{
			_eventQueued = null;
			_triggered = false;
			_enterCount++;
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
				throw new ArgumentNullException(nameof(action), $"The state {Name} cannot have a null OnEnter action");
			}

			_onEnter.Add(action);
		}

		/// <inheritdoc />
		public void OnExit(Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action), $"The state {Name} cannot have a null OnExit action");
			}

			_onExit.Add(action);
		}

		/// <inheritdoc />
		public ITransition WaitingFor(Func<Task> taskAwaitAction)
		{
			ThrowIfNull(taskAwaitAction);

			return SetAwaitAction(_ => AwaitTask(taskAwaitAction()));
		}

		/// <inheritdoc />
		public ITransition WaitingFor(Func<UniTask> taskAwaitAction)
		{
			ThrowIfNull(taskAwaitAction);

			return SetAwaitAction(_ => taskAwaitAction());
		}

		/// <inheritdoc />
		public ITransition WaitingFor(Func<CancellationToken, Task> taskAwaitAction)
		{
			ThrowIfNull(taskAwaitAction);

			return SetAwaitAction(token => AwaitTask(taskAwaitAction(token)));
		}

		/// <inheritdoc />
		public ITransition WaitingFor(Func<CancellationToken, UniTask> taskAwaitAction)
		{
			ThrowIfNull(taskAwaitAction);

			return SetAwaitAction(taskAwaitAction);
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (!_triggered)
			{
				_triggered = true;
				InnerTaskAwait(_enterCount, Chart.CancellationToken).Forget();
			}

			return Completed ? _transition : null;
		}

		private void ThrowIfNull(Delegate taskAwaitAction)
		{
			if (taskAwaitAction == null)
			{
				throw new ArgumentNullException(nameof(taskAwaitAction), $"The state {Name} cannot have a null wait action");
			}
		}

		private ITransition SetAwaitAction(Func<CancellationToken, UniTask> taskAwaitAction)
		{
			_taskAwaitAction = taskAwaitAction;
			_transition = CreateTransition(TransitionKind.Completion);

			return _transition;
		}

		private static async UniTask AwaitTask(Task task)
		{
			// Awaiting the Task directly resumes inline when it completes on the main thread; AsUniTask would always
			// post the continuation to a later frame.
			await task;
		}

		private async UniTaskVoid InnerTaskAwait(int enterCount, CancellationToken token)
		{
			try
			{
				await _taskAwaitAction(token);
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				// Cancelled by Reset or Dispose: the state was abandoned, so there is nothing left to complete.
				return;
			}
			catch (Exception e)
			{
				// A faulted task still completes the state; otherwise the chart and any parent split would wait forever.
				ReportFault(e, $"Exception in the state '{Name}', when awaiting the task wait action.", CreationStackTrace);
			}

			// Task continuations may resume on a worker thread; the chart and its callbacks only run on the main thread.
			await UniTask.SwitchToMainThread();

			if (enterCount != _enterCount || token.IsCancellationRequested)
			{
				return;
			}

			Completed = true;

			_stateFactory.Data.StateChartMoveNextCall(_eventQueued);
		}
	}
}
