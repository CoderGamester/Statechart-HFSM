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
		RegionNode RootRegion { get; }

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
		void GetActiveStates(List<StateNode> results);

		/// <summary>
		/// Returns true when <paramref name="state"/> is currently active.
		/// </summary>
		bool IsActive(StateNode state);
	}

	/// <inheritdoc cref="IStatechart"/>
	public class Statechart : IStatechart
	{
		private static uint _idRef;

		private bool _isRunning;
		private bool _isProcessing;
		private bool _isDisposed;
		private int _generation;
		private IStateInternal _currentState;
		private CancellationTokenSource _cancellation = new CancellationTokenSource();

		private readonly IStateFactoryInternal _stateFactory;
		private readonly Queue<IStatechartEvent> _pendingEvents = new Queue<IStatechartEvent>();
		private readonly List<StateNode> _activeBuffer = new List<StateNode>();

		/// <inheritdoc />
		public bool LogsEnabled { get; set; }
		/// <inheritdoc />
		public string Name { get; }
		/// <inheritdoc />
		public bool IsRunning => _isRunning;
		/// <inheritdoc />
		public RegionNode RootRegion { get; }

		/// <inheritdoc />
		public event Action<StatechartTrace> Traced;

		/// <summary>Changes on every Reset/Dispose so stale wait and task completions can be ignored.</summary>
		internal int Generation => _generation;
		/// <summary>Cancelled on Reset/Dispose; passed to token-aware task waits.</summary>
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

			Name = string.IsNullOrEmpty(name) ? $"Statechart {++_idRef}" : name;

			var stateFactory = new StateFactory(0, new StateFactoryData { Statechart = this, StateChartMoveNextCall = MoveNext });

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
			RootRegion = StatechartTopologyBuilder.Build(_stateFactory);

#if UNITY_EDITOR
			StatechartRegistry.Register(this);
#endif
		}

		/// <inheritdoc />
		public void Trigger(IStatechartEvent trigger)
		{
			if (!_isRunning)
			{
				return;
			}

			MoveNext(trigger);
		}

		/// <inheritdoc />
		public void Run()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(Name);
			}

			_isRunning = true;

			Emit(new StatechartTrace(this, StatechartTraceKind.Started));
			MoveNext(null);
		}

		/// <inheritdoc />
		public void Pause()
		{
			if (!_isRunning)
			{
				return;
			}

			_isRunning = false;

			Emit(new StatechartTrace(this, StatechartTraceKind.Paused));
		}

		/// <inheritdoc />
		public void Reset()
		{
			if (_isDisposed)
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
			if (_isDisposed)
			{
				return;
			}

			_isRunning = false;
			_isDisposed = true;

			AbandonPendingWork();
			Emit(new StatechartTrace(this, StatechartTraceKind.Disposed));

			Traced = null;

#if UNITY_EDITOR
			StatechartRegistry.Unregister(this);
#endif
		}

		/// <inheritdoc />
		public void GetActiveStates(List<StateNode> results)
		{
			if (results == null)
			{
				throw new ArgumentNullException(nameof(results));
			}

			results.Clear();
			AddActive(_currentState, results);
		}

		/// <inheritdoc />
		public bool IsActive(StateNode state)
		{
			if (state == null)
			{
				return false;
			}

			GetActiveStates(_activeBuffer);

			return _activeBuffer.Contains(state);
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

		private static void AddActive(IStateInternal state, List<StateNode> results)
		{
			results.Add(state.Node);

			if (state is not SplitState split)
			{
				return;
			}

			for (var i = 0; i < split.Regions.Count; i++)
			{
				var current = split.Regions[i].CurrenState;

				if (current != null)
				{
					AddActive(current, results);
				}
			}
		}

		private void AbandonPendingWork()
		{
			_generation++;
			_pendingEvents.Clear();
			_cancellation.Cancel();
			_cancellation.Dispose();
		}

		private void MoveNext(IStatechartEvent trigger)
		{
			if (!_isRunning)
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

				while (_isRunning && _pendingEvents.Count > 0)
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
			var generation = _generation;

			if (trigger != null)
			{
				Emit(new StatechartTrace(this, StatechartTraceKind.EventTriggered, _currentState.Node, null, trigger));
			}

			var nextState = _currentState.Trigger(trigger);

			while (nextState != null && generation == _generation)
			{
				_currentState = nextState;

				if (!_isRunning)
				{
					// Paused from a callback: the entered state is processed again on the next Run().
					return;
				}

				nextState = _currentState.Trigger(null);
			}
		}
	}
}