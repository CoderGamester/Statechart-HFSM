using System;
using System.Collections.Generic;
using GameLovers.Statechart;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc cref="IWaitState"/>
	internal class WaitState : StateInternal, IWaitState
	{
		private ITransitionInternal _transition;
		private IWaitActivityInternal _waitingActivity;
		private Action<IWaitActivity> _waitAction;
		private bool _initialized;

		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly Dictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public WaitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_initialized = false;
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_waitingActivity.Reset();
			_initialized = false;

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
			if (_waitingActivity == null)
			{
				throw new MissingMethodException($"The state {Name} doesn't have a waiting activity");
			}

			if (_transition.TargetState == null && _events.Count == 0)
			{
				throw new MissingMemberException($"The state {Name} doesn't have a target state in it's transition");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}

			foreach (var eventTransition in _events)
			{
				if (eventTransition.Value.TargetState.Id == Id)
				{
					throw new InvalidOperationException(
						$"The state {Name} with the event {eventTransition.Key.Name} is pointing to itself on transition");
				}
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
		public ITransition WaitingFor(Action<IWaitActivity> waitAction)
		{
			if (waitAction == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null wait action");
			}

			_waitingActivity = new WaitActivity(_stateFactory.Data.StateChartMoveNextCall);
			_transition = new Transition();

			_waitAction = waitAction;

			return _transition;
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out ITransitionInternal transition))
			{
				return transition;
			}

			if (!_initialized)
			{
				_waitAction(_waitingActivity);
				_initialized = true;
			}

			return _waitingActivity.IsCompleted ? _transition : null;
		}
	}
}
