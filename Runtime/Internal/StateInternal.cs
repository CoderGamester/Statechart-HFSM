using System;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc cref="IState"/>
	internal interface IStateInternal : IState, IEquatable<IStateInternal>
	{
		/// <summary>
		/// The unique value identifying this state
		/// </summary>
		uint Id { get; }
		/// <summary>
		/// The string representation identifying this state
		/// </summary>
		string Name { get; }
		/// <summary>
		/// The layer in the nested setup this state is in. If in the root then the value will be 0
		/// </summary>
		uint RegionLayer { get; }
		/// <summary>
		/// The stack trace when this setup was created. Relevant for debugging purposes
		/// </summary>
		string CreationStackTrace { get; }
		
		/// <summary>
		/// Triggers the given <paramref name="statechartEvent"/> as input to the <see cref="IStateMachine"/> and returns
		/// the processed <see cref="IStateInternal"/> as an output
		/// </summary>
		IStateInternal Trigger(IStatechartEvent statechartEvent);
		/// <summary>
		/// Marks the initial moment of this state as the new current state in the <see cref="IStateMachine"/>
		/// </summary>
		void Enter();
		/// <summary>
		/// Marks the final moment of this state as the current state in the <see cref="IStateMachine"/>
		/// </summary>
		void Exit();
		/// <summary>
		/// Validates this state to any potential bad setup schemes. Relevant to debug purposes.
		/// It requires the <see cref="IStateMachine"/> to run at runtime.
		/// </summary>
		void Validate();
	}

	/// <inheritdoc />
	internal abstract class StateInternal : IStateInternal
	{
		protected readonly IStateFactoryInternal _stateFactory;

		private static uint _idRef;

		/// <inheritdoc />
		public bool LogsEnabled { get; set; }
		/// <inheritdoc />
		public uint Id { get; }
		/// <inheritdoc />
		public string Name { get; }
		/// <inheritdoc />
		public uint RegionLayer => _stateFactory.RegionLayer;
		/// <inheritdoc />
		public string CreationStackTrace { get; }

		protected bool IsStateLogsEnabled => LogsEnabled || _stateFactory.Data.StateMachine.LogsEnabled;

		protected StateInternal(string name, IStateFactoryInternal stateFactory)
		{
			Id = ++_idRef;
			Name = name;

			_stateFactory = stateFactory;

#if UNITY_EDITOR || DEBUG
			CreationStackTrace = StatechartUtils.RemoveGarbageFromStackTrace(Environment.StackTrace);
#endif
		}

		/// <inheritdoc />
		public IStateInternal Trigger(IStatechartEvent statechartEvent)
		{
			var transition = OnTrigger(statechartEvent);

			if (transition == null)
			{
				if (IsStateLogsEnabled)
				{
					Debug.Log($"'{statechartEvent?.Name}' does not trigger any transition in state '{Name}'");
				}
				
				return null;
			}

			var nextState = transition.TargetState;

			if (Equals(nextState))
			{
				if (IsStateLogsEnabled)
				{
					Debug.Log($"'{statechartEvent?.Name}' event triggers the transition in state'{Name}' to" +
					          $"itself because it's of type {GetType().UnderlyingSystemType.Name}");
				}
				
				return nextState;
			}

			if (nextState == null)
			{
				TriggerTransition(transition, statechartEvent?.Name);

				return null;
			}

			TriggerExit();
			TriggerTransition(transition, statechartEvent?.Name);
			TriggerEnter(nextState);

			return nextState;
		}

		/// <inheritdoc />
		public bool Equals(IStateInternal stateInternal)
		{
			return stateInternal != null && Id == stateInternal.Id;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is IStateInternal stateBase && Equals(stateBase);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return (int) Id;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Name;
		}

		/// <inheritdoc />
		public abstract void Enter();
		/// <inheritdoc />
		public abstract void Exit();
		/// <inheritdoc />
		public abstract void Validate();

		protected abstract ITransitionInternal OnTrigger(IStatechartEvent statechartEvent);

		private void TriggerEnter(IStateInternal state)
		{
			try
			{
				if (IsStateLogsEnabled)
				{
					Debug.Log($"Entering '{state.Name}'");
				}
				state.Enter();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the state {state.Name}, OnEnter() actions.\n" + CreationStackTrace, e);
			}
		}

		private void TriggerExit()
		{
			try
			{
				if(IsStateLogsEnabled)
				{
					Debug.LogFormat($"Exiting '{Name}'");
				}
				Exit();
			}
			catch(Exception e)
			{
				throw new Exception($"Exception in the state '{Name}', OnExit() actions.\n" + CreationStackTrace, e);
			}
		}

		private void TriggerTransition(ITransitionInternal transition, string eventName)
		{
			try
			{
				if (IsStateLogsEnabled)
				{
					var targetState = transition.TargetState?.Name ?? "only invokes OnTransition() without moving to new state";
					
					Debug.Log($"'{eventName}' event triggers the transition '{Name}' -> '{targetState}'");
				}
				
				transition.TriggerTransition();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the transition '{Name}' -> '{transition.TargetState?.Name}'," +
				                    $" TriggerTransition() actions.\n{transition.CreationStackTrace}", e);
			}
		}
	}
}