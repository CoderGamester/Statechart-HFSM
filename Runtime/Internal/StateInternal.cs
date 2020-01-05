using System;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc cref="IState"/>
	internal interface IStateInternal : IState, IEquatable<IStateInternal>
	{
		uint Id { get; }
		string Name { get; }
		uint RegionLayer { get; }
		string CreationStackTrace { get; }
		
		IStateInternal Trigger(IStatechartEvent statechartEvent);
		void Enter();
		void Exit();
		void Validate();
	}

	/// <inheritdoc />
	internal abstract class StateInternal : IStateInternal
	{
		protected readonly IStateFactoryInternal _stateFactory;

		private static uint _idRef;

		/// <inheritdoc />
		public uint Id { get; private set; }
		/// <inheritdoc />
		public string Name { get; private set; }
		/// <inheritdoc />
		public uint RegionLayer { get { return _stateFactory.RegionLayer; } }
		/// <inheritdoc />
		public string CreationStackTrace { get; private set; }

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

		public bool Equals(IStateInternal stateInternal)
		{
			return stateInternal != null && Id == stateInternal.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is IStateInternal stateBase && Equals(stateBase);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}

		public override string ToString()
		{
			return Name;
		}

		public abstract void Enter();
		public abstract void Exit();
		public abstract void Validate();

		protected abstract ITransitionInternal OnTrigger(IStatechartEvent statechartEvent);
	}
}