using System;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine
{
	/// <summary>
	/// The kind of step a <see cref="StatechartTrace"/> records.
	/// </summary>
	public enum StatechartTraceKind
	{
		Started,        // Run() started or resumed the chart
		Paused,         // Pause() stopped the chart
		Reset,          // Reset() moved the chart back to its initial state
		Disposed,       // Dispose() released the chart
		EventTriggered, // An event started processing
		StateExited,    // A state is about to run its exit callbacks
		Transition,     // A transition is about to run its transition callbacks
		StateEntered,   // A state is about to run its enter callbacks
		Faulted         // A user callback, wait action or awaited task threw
	}

	/// <summary>
	/// One observable step of a running <see cref="IStatechart"/>, published through
	/// <see cref="IStatechart.Traced"/> in execution order.
	/// </summary>
	public readonly struct StatechartTrace
	{
		/// <summary>
		/// The statechart that produced this step.
		/// </summary>
		public IStatechart Statechart { get; }
		/// <summary>
		/// What happened.
		/// </summary>
		public StatechartTraceKind Kind { get; }
		/// <summary>
		/// The state the step is about: the exited, entered or faulted state, or a transition's source.
		/// </summary>
		public StateNode State { get; }
		/// <summary>
		/// The target of a <see cref="StatechartTraceKind.Transition"/>; null for action-only transitions and for
		/// every other kind.
		/// </summary>
		public StateNode Target { get; }
		/// <summary>
		/// The event being processed when the step happened, or null when the chart moved on its own.
		/// </summary>
		public IStatechartEvent Event { get; }
		/// <summary>
		/// The exception of a <see cref="StatechartTraceKind.Faulted"/> step; null otherwise.
		/// </summary>
		public Exception Exception { get; }

		internal StatechartTrace(IStatechart statechart, StatechartTraceKind kind, StateNode state = null,
		                         StateNode target = null, IStatechartEvent statechartEvent = null,
		                         Exception exception = null)
		{
			Statechart = statechart;
			Kind = kind;
			State = state;
			Target = target;
			Event = statechartEvent;
			Exception = exception;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var prefix = $"[Statechart '{Statechart?.Name}']";
			var eventSuffix = Event == null ? "" : $" on '{Event.Name}'";

			switch (Kind)
			{
				case StatechartTraceKind.EventTriggered:
					return $"{prefix} event '{Event?.Name}'";
				case StatechartTraceKind.StateExited:
					return $"{prefix} exit {State?.Path}";
				case StatechartTraceKind.StateEntered:
					return $"{prefix} enter {State?.Path}";
				case StatechartTraceKind.Transition:
					return $"{prefix} {State?.Path} -> {Target?.Path ?? "(action only)"}{eventSuffix}";
				case StatechartTraceKind.Faulted:
					return $"{prefix} fault in {State?.Path}: {Exception?.GetType().Name} {Exception?.Message}";
				default:
					return $"{prefix} {Kind}";
			}
		}
	}
}
