using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <inheritdoc cref="IWaitState"/>
	internal class WaitState : StateInternal, IWaitState
	{
		private ITransitionInternal _transition;
		private IWaitActivityInternal _waitingActivity;
		private Action<IWaitActivity> _waitAction;
		private bool _triggered;

		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly Dictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public WaitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_triggered = false;
		}

		/// <summary>
		/// Forces this state to complete, bypassing the normal flow of exeuction. 
		/// This is helpful if a parent nested state is controling this state
		/// </summary>
		public void ForceComplete()
		{
			_waitingActivity.ForceComplete();
		}

		/// <inheritdoc />
		public override void Enter()
		{
			// It needs to create a new activity everytime on to make sure old activities don't override new ones
			_waitingActivity = new WaitActivity(OnActivityComplete);
			_triggered = false;

			for (int i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			for (int i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_waitAction == null)
			{
				throw new InvalidOperationException($"The state {Name} doesn't have a waiting activity");
			}

			if (_transition?.TargetState == null)
			{
				throw new InvalidOperationException($"The state {Name} is not pointing to any state");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}

			foreach (var eventTransition in _events)
			{
				if (eventTransition.Value.TargetState?.Id == Id)
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
			_waitAction = waitAction ?? throw new NullReferenceException($"The state {Name} cannot have a null wait action");
			_transition = new Transition();

			return _transition;
		}

		private void OnActivityComplete(uint id)
		{
			if (id == _waitingActivity.Id)
			{
				_stateFactory.Data.StateChartMoveNextCall(null);
			}
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out var transition))
			{
				if (transition.TargetState != null)
				{
					_waitingActivity.ForceComplete();
				}

				return transition;
			}

			if (!_triggered)
			{
				_triggered = true;
				InnerWait(statechartEvent?.Name);
			}

			return _waitingActivity.IsCompleted ? _transition : null;
		}

		private void InnerWait(string eventName)
		{
			try
			{
				if (IsStateLogsEnabled)
				{
					Debug.Log($"'{eventName}' event triggers the wait method '{_waitAction.Method.Name}'" +
							  $"from the object {_waitAction.Target} in the state {Name}");
				}
				_waitAction(_waitingActivity);
			}
			catch (Exception e)
			{
				var finalMessage = "";
#if UNITY_EDITOR || DEBUG
				finalMessage = $"\nStackTrace log of '{Name}' state creation bellow.\n{CreationStackTrace}";
#endif

				Debug.LogError($"Exception in the state '{Name}', when calling the wait action {_waitAction.Method.Name}" +
					$" from the object {_waitAction.Target}.\n" +
					$"-->> Check the exception log after this one for more details <<-- {finalMessage}");
				Debug.LogException(e);
			}
		}
	}
}
