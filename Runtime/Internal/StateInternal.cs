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
		/// Triggers the given <paramref name="statechartEvent"/> as input to the <see cref="IStatechart"/> and returns
		/// the processed <see cref="IStateInternal"/> as an output
		/// </summary>
		IStateInternal Trigger(IStatechartEvent statechartEvent);
		/// <summary>
		/// Marks the initial moment of this state as the new current state in the <see cref="IStatechart"/>
		/// </summary>
		void Enter();
		/// <summary>
		/// Marks the final moment of this state as the current state in the <see cref="IStatechart"/>
		/// </summary>
		void Exit();
		/// <summary>
		/// Validates this state to any potential bad setup schemes. Relevant to debug purposes.
		/// It requires the <see cref="IStatechart"/> to run at runtime.
		/// </summary>
		void Validate();
	}

	/// <inheritdoc />
	internal abstract class StateInternal : IStateInternal
	{
		protected readonly IStateFactoryInternal _stateFactory;

		private static uint _idRef;

		/// <inheritdoc />
		public uint Id { get; }
		/// <inheritdoc />
		public string Name { get; }
		/// <inheritdoc />
		public uint RegionLayer => _stateFactory.RegionLayer;
		/// <inheritdoc />
		public string CreationStackTrace { get; }

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
				return null;
			}

			var nextState = transition.TargetState;

			if (Equals(nextState))
			{
				return nextState;
			}
			
			try
			{
				if(_stateFactory.Data.Statechart.LogsEnabled)
				{
					Debug.LogFormat("Exiting '{0}'", Name);
				}
				Exit();
			}
			catch(Exception e)
			{
				throw new Exception($"Exception in the state '{Name}', OnExit() actions.\n" + CreationStackTrace, e);
			}

			try
			{
				if (_stateFactory.Data.Statechart.LogsEnabled)
				{
					Debug.LogFormat("Transition '{0}' -> '{1}'", Name, nextState.Name);
				}
				transition.TriggerTransition();
			}
			catch (Exception e)
			{
				throw new Exception(
					$"Exception in the transition '{Name}' -> '{nextState.Name}', TriggerTransition() actions.\n" + transition.CreationStackTrace, e);
			}

			try
			{
				if (_stateFactory.Data.Statechart.LogsEnabled)
				{
					Debug.LogFormat("Entering '{0}'", nextState.Name);
				}
				nextState.Enter();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the state {nextState.Name}, OnEnter() actions.\n" + CreationStackTrace, e);
			}

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
	}
}