using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
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
		/// The kind of state, as exposed by the public topology
		/// </summary>
		StateKind Kind { get; }
		/// <summary>
		/// The layer in the nested setup this state is in. If in the root then the value will be 0
		/// </summary>
		uint RegionLayer { get; }
		/// <summary>
		/// The factory that created this state, identifying its region
		/// </summary>
		IStateFactoryInternal Factory { get; }
		/// <summary>
		/// The public description of this state, assigned once the statechart topology is built
		/// </summary>
		StateNode Node { get; set; }
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
		/// Traces and runs <see cref="Enter"/>, reporting any exception instead of throwing it
		/// </summary>
		void EnterSafely(IStatechartEvent statechartEvent);
		/// <summary>
		/// Traces and runs <see cref="Exit"/>, reporting any exception instead of throwing it
		/// </summary>
		void ExitSafely(IStatechartEvent statechartEvent);
		/// <summary>
		/// Validates this state to any potential bad setup schemes. Relevant to debug purposes.
		/// It requires the <see cref="IStatechart"/> to run at runtime.
		/// </summary>
		void Validate();
		/// <summary>
		/// Adds every outgoing transition of this state to <paramref name="results"/>
		/// </summary>
		void CollectTransitions(List<TransitionInfo> results);
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
		public abstract StateKind Kind { get; }
		/// <inheritdoc />
		public uint RegionLayer => _stateFactory.RegionLayer;
		/// <inheritdoc />
		public IStateFactoryInternal Factory => _stateFactory;
		/// <inheritdoc />
		public StateNode Node { get; set; }
		/// <inheritdoc />
		public string CreationStackTrace { get; }

		/// <summary>The statechart that owns this state.</summary>
		protected Statechart Chart => _stateFactory.Data.Statechart;

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

			if (nextState == null)
			{
				TriggerTransition(transition, statechartEvent);

				return null;
			}

			ExitSafely(statechartEvent);
			TriggerTransition(transition, statechartEvent);
			nextState.EnterSafely(statechartEvent);

			return nextState;
		}

		/// <inheritdoc />
		public void EnterSafely(IStatechartEvent statechartEvent)
		{
			Trace(StatechartTraceKind.StateEntered, null, statechartEvent);

			try
			{
				Enter();
			}
			catch (Exception e)
			{
				ReportFault(e, $"Exception in the state '{Name}', OnEnter() actions.", CreationStackTrace);
			}
		}

		/// <inheritdoc />
		public void ExitSafely(IStatechartEvent statechartEvent)
		{
			Trace(StatechartTraceKind.StateExited, null, statechartEvent);

			try
			{
				Exit();
			}
			catch (Exception e)
			{
				ReportFault(e, $"Exception in the state '{Name}', OnExit() actions.", CreationStackTrace);
			}
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
		/// <inheritdoc />
		public abstract void CollectTransitions(List<TransitionInfo> results);

		/// <summary>
		/// Returns the transition this state takes for the given event, or null when it does not handle it.
		/// </summary>
		protected abstract ITransitionInternal OnTrigger(IStatechartEvent statechartEvent);

		/// <summary>
		/// Publishes a trace about this state when the chart has a listener or this state's logs are enabled.
		/// </summary>
		protected void Trace(StatechartTraceKind kind, StateNode target, IStatechartEvent statechartEvent,
		                     Exception exception = null)
		{
			var chart = Chart;

			if (!chart.HasTraceListeners && !LogsEnabled)
			{
				return;
			}

			chart.Emit(new StatechartTrace(chart, kind, Node, target, statechartEvent, exception), LogsEnabled);
		}

		/// <summary>
		/// Logs a user-code exception with the setup stack trace that declared the failing element, and traces it.
		/// </summary>
		protected void ReportFault(Exception exception, string message, string creationStackTrace)
		{
			var finalMessage = "";
#if UNITY_EDITOR || DEBUG
			finalMessage = $"\nStackTrace log of the failing element's creation below.\n{creationStackTrace}";
#endif
			Debug.LogError($"{message}\n-->> Check the exception log after this one for more details <<-- {finalMessage}");
			Debug.LogException(exception);
			Trace(StatechartTraceKind.Faulted, null, null, exception);
		}

		private void TriggerTransition(ITransitionInternal transition, IStatechartEvent statechartEvent)
		{
			Trace(StatechartTraceKind.Transition, transition.TargetState?.Node, statechartEvent);

			try
			{
				transition.TriggerTransition();
			}
			catch (Exception e)
			{
				ReportFault(e, $"Exception in the transition '{Name}' -> '{transition.TargetState?.Name}'.",
					transition.CreationStackTrace);
			}
		}
	}
}