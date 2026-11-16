using System;
using System.Collections.Generic;
using System.Threading;
using GameLovers.StatechartMachine.Internal;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine
{
	/// <summary>
	/// Interface to help debug the state chart
	/// </summary>
	public interface IStateMachineDebug
	{
		/// <summary>
		/// Writes every <see cref="StatechartTrace"/> of this chart to the Unity console.
		/// </summary>
		bool LogsEnabled { get; set; }
	}

	/// <summary>
	/// The main object which represents the State Chart and drives it forward.
	/// The State Chart schematics are defined in the constructor setup action and cannot be modified during runtime.
	/// Events are processed with run-to-completion semantics: an event triggered while another one is being processed,
	/// for example from an enter or transition callback, is queued and processed right after the current one.
	/// </summary>
	public interface IStatechart : IStateMachineDebug, IDisposable
	{
		/// <summary>
		/// The name used by logs, traces and debugging tools.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// True after <see cref="Run"/> until <see cref="Pause"/> or <see cref="IDisposable.Dispose"/>.
		/// </summary>
		bool IsRunning { get; }

		/// <summary>
		/// The read-only topology of the chart, starting at its root region.
		/// </summary>
		IRegionNode RootRegion { get; }

		/// <summary>
		/// Publishes every observable step of the chart in execution order.
		/// No trace is allocated or published while nothing subscribes and logs are disabled.
		/// </summary>
		event Action<StatechartTrace> Traced;

		/// <summary>
		/// Processes the event <paramref name="trigger"/> for the State Chart with run-to-completion paradigm.
		/// Triggers only work if the State Chart is running.
		/// </summary>
		void Trigger(IStatechartEvent trigger);

		/// <summary>
		/// Start/Resume the control of the State Chart from where is anchored.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown when the chart was disposed.</exception>
		void Run();

		/// <summary>
		/// Pauses the control of the State Chart.
		/// Call <see cref="Run"/> to resume it.
		/// </summary>
		void Pause();

		/// <summary>
		/// Resets the State Chart to it's initial starting point without running any exit callback.
		/// Pending events are dropped and outstanding waits and tasks are abandoned: their completion no longer moves
		/// the chart, and tasks awaited with a <see cref="CancellationToken"/> are cancelled.
		/// This call doesn't pause or resume the control of the State Chart.
		/// </summary>
		void Reset();

		/// <summary>
		/// Fills <paramref name="results"/> with every active state, parents before their children.
		/// Before <see cref="Run"/> the only active state is the root initial state.
		/// </summary>
		void GetActiveStates(List<IStateNode> results);

		/// <summary>
		/// Returns true when <paramref name="state"/> is currently active.
		/// </summary>
		bool IsActive(IStateNode state);
	}

	/// <inheritdoc cref="IStatechart"/>
	public class Statechart : IStatechart
	{
		private enum Lifecycle
		{
			Paused,
			Running,
			Disposed
		}

#if UNITY_EDITOR
		/// <summary>Raised after every chart is constructed, so the Statechart Debugger can discover it.</summary>
		internal static event Action<Statechart> Created;
#endif

		private Lifecycle _lifecycle;
		private bool _isProcessing;
		private IStateInternal _currentState;
		private CancellationTokenSource _cancellation = new CancellationTokenSource();

		private readonly IStateFactoryInternal _stateFactory;
		private readonly Queue<IStatechartEvent> _pendingEvents = new Queue<IStatechartEvent>();

		/// <inheritdoc />
		public bool LogsEnabled { get; set; }
		/// <inheritdoc />
		public string Name { get; }
		/// <inheritdoc />
		public bool IsRunning => _lifecycle == Lifecycle.Running;
		/// <inheritdoc />
		public IRegionNode RootRegion => _stateFactory;

		/// <inheritdoc />
		public event Action<StatechartTrace> Traced;

		/// <summary>
		/// Cancelled on Reset/Dispose. Waits and tasks capture it when they start and ignore their completion once it
		/// is cancelled, and token-aware task waits receive it.
		/// </summary>
		internal CancellationToken CancellationToken => _cancellation.Token;
		/// <summary>True when a trace would reach a subscriber or the console.</summary>
		internal bool HasTraceListeners => Traced != null || LogsEnabled;

		public Statechart(Action<IStateFactory> setup) : this(null, setup)
		{
		}

		public Statechart(string name, Action<IStateFactory> setup)
		{
			if (setup == null)
			{
				throw new ArgumentNullException(nameof(setup));
			}

			Name = string.IsNullOrEmpty(name) ? nameof(Statechart) : name;

			var stateFactory = new StateFactory(null, 0, 0, new StateFactoryData { Statechart = this, StateChartMoveNextCall = MoveNext });

			setup(stateFactory);

			_stateFactory = stateFactory;

			if (_stateFactory.InitialState == null)
			{
				throw new InvalidOperationException($"State chart {Name} doesn't have initial state");
			}

			_currentState = _stateFactory.InitialState;

#if UNITY_EDITOR || DEBUG
			for(int i = 0; i < _stateFactory.States.Count; i++)
			{
				_stateFactory.States[i].Validate();
			}
#endif
#if UNITY_EDITOR
			Created?.Invoke(this);
#endif
		}

		/// <inheritdoc />
		public void Trigger(IStatechartEvent trigger)
		{
			if (!IsRunning)
			{
				return;
			}

			MoveNext(trigger);
		}

		/// <inheritdoc />
		public void Run()
		{
			if (_lifecycle == Lifecycle.Disposed)
			{
				throw new ObjectDisposedException(Name);
			}

			_lifecycle = Lifecycle.Running;

			Emit(new StatechartTrace(this, StatechartTraceKind.Started));
			MoveNext(null);
		}

		/// <inheritdoc />
		public void Pause()
		{
			if (!IsRunning)
			{
				return;
			}

			_lifecycle = Lifecycle.Paused;

			Emit(new StatechartTrace(this, StatechartTraceKind.Paused));
		}

		/// <inheritdoc />
		public void Reset()
		{
			if (_lifecycle == Lifecycle.Disposed)
			{
				return;
			}

			AbandonPendingWork();
			_cancellation = new CancellationTokenSource();
			_currentState = _stateFactory.InitialState;

			Emit(new StatechartTrace(this, StatechartTraceKind.Reset));
		}

		/// <summary>
		/// Stops the chart for good: pauses it, abandons pending waits and tasks, cancels token-aware tasks and
		/// unregisters it from debugging tools. Calling it again does nothing.
		/// </summary>
		public void Dispose()
		{
			if (_lifecycle == Lifecycle.Disposed)
			{
				return;
			}

			_lifecycle = Lifecycle.Disposed;

			AbandonPendingWork();
			Emit(new StatechartTrace(this, StatechartTraceKind.Disposed));

			Traced = null;
		}

		/// <inheritdoc />
		public void GetActiveStates(List<IStateNode> results)
		{
			if (results == null)
			{
				throw new ArgumentNullException(nameof(results));
			}

			results.Clear();
			AddActive(_currentState, results);
		}

		/// <inheritdoc />
		public bool IsActive(IStateNode state)
		{
			return state != null && IsActiveWithin(_currentState, state);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Name;
		}

		/// <summary>Publishes <paramref name="trace"/> to subscribers and, when logging is on, the console.</summary>
		internal void Emit(in StatechartTrace trace, bool forceLog = false)
		{
			Traced?.Invoke(trace);

			if (trace.Kind == StatechartTraceKind.Faulted)
			{
				// Faults are always reported by the throwing site; repeating them here would double every error.
				return;
			}

			if (LogsEnabled || forceLog)
			{
				Debug.Log(trace.ToString());
			}
		}

		private static void AddActive(IStateInternal state, List<IStateNode> results)
		{
			results.Add(state);

			if (state is not SplitState split)
			{
				return;
			}

			for (var i = 0; i < split.InnerStates.Count; i++)
			{
				var current = split.InnerStates[i].CurrenState;

				if (current != null)
				{
					AddActive(current, results);
				}
			}
		}

		private static bool IsActiveWithin(IStateInternal current, IStateNode state)
		{
			if (ReferenceEquals(current, state))
			{
				return true;
			}

			if (current is not SplitState split)
			{
				return false;
			}

			for (var i = 0; i < split.InnerStates.Count; i++)
			{
				var inner = split.InnerStates[i].CurrenState;

				if (inner != null && IsActiveWithin(inner, state))
				{
					return true;
				}
			}

			return false;
		}

		private void AbandonPendingWork()
		{
			_pendingEvents.Clear();
			// Not disposed: abandoned waits still read their captured token, and a timer-free source owns nothing.
			_cancellation.Cancel();
		}

		private void MoveNext(IStatechartEvent trigger)
		{
			if (!IsRunning)
			{
				return;
			}

			if (_isProcessing)
			{
				// Several completions can request a re-evaluation in one step; one queued null trigger covers them all.
				if (trigger != null || !_pendingEvents.Contains(null))
				{
					_pendingEvents.Enqueue(trigger);
				}

				return;
			}

			_isProcessing = true;

			try
			{
				Process(trigger);

				while (IsRunning && _pendingEvents.Count > 0)
				{
					Process(_pendingEvents.Dequeue());
				}
			}
			finally
			{
				_isProcessing = false;
			}
		}

		private void Process(IStatechartEvent trigger)
		{
			var token = _cancellation.Token;

			if (trigger != null)
			{
				Emit(new StatechartTrace(this, StatechartTraceKind.EventTriggered, _currentState, null, trigger));
			}

			var nextState = _currentState.Trigger(trigger);

			while (nextState != null && !token.IsCancellationRequested)
			{
				_currentState = nextState;

				if (!IsRunning)
				{
					// Paused from a callback: the entered state is processed again on the next Run().
					return;
				}

				nextState = _currentState.Trigger(null);
			}
		}
	}
}