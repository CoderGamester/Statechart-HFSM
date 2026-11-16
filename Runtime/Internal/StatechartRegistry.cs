#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <summary>
	/// A trace step copied without a reference to its statechart, so recorded history never keeps a chart alive.
	/// </summary>
	internal readonly struct StatechartTraceRecord
	{
		public readonly double Time;
		public readonly int Frame;
		public readonly StatechartTraceKind Kind;
		public readonly StateNode State;
		public readonly StateNode Target;
		public readonly string EventName;
		public readonly string Message;

		public StatechartTraceRecord(in StatechartTrace trace, double time, int frame)
		{
			Time = time;
			Frame = frame;
			Kind = trace.Kind;
			State = trace.State;
			Target = trace.Target;
			EventName = trace.Event?.Name;
			Message = trace.ToString();
		}
	}

	/// <summary>
	/// Editor-only list of live statecharts and their recent trace history, read by the Statechart Debugger.
	/// Charts are held weakly, so a chart that is never disposed still disappears once it is collected.
	/// </summary>
	internal static class StatechartRegistry
	{
		internal const int HistoryCapacity = 256;

		private static readonly List<Entry> _entries = new List<Entry>();

		/// <summary>Raised on the main thread after a chart is added or removed.</summary>
		internal static event Action Changed;

		/// <summary>Increments on every recorded trace of any chart, so views can repaint only on change.</summary>
		internal static int Version { get; private set; }

		/// <summary>
		/// One registered chart and its recent trace history, oldest first.
		/// </summary>
		internal sealed class Entry
		{
			private readonly WeakReference<IStatechart> _chart;
			private readonly Queue<StatechartTraceRecord> _history = new Queue<StatechartTraceRecord>();

			/// <summary>The chart name captured at registration.</summary>
			internal string Name { get; }
			/// <summary>The recent trace steps, oldest first, capped at <see cref="HistoryCapacity"/>.</summary>
			internal IReadOnlyCollection<StatechartTraceRecord> History => _history;

			internal Entry(IStatechart chart)
			{
				_chart = new WeakReference<IStatechart>(chart);
				Name = chart.Name;
			}

			/// <summary>Returns the chart while it is still alive.</summary>
			internal bool TryGetChart(out IStatechart chart)
			{
				return _chart.TryGetTarget(out chart);
			}

			/// <summary>Appends a copy of <paramref name="trace"/>, dropping the oldest step when full.</summary>
			internal void Record(StatechartTrace trace)
			{
				if (_history.Count == HistoryCapacity)
				{
					_history.Dequeue();
				}

				_history.Enqueue(new StatechartTraceRecord(trace, Time.realtimeSinceStartupAsDouble, Time.frameCount));
				Version++;
			}
		}

		/// <summary>
		/// Returns the live entries, dropping charts that were collected.
		/// </summary>
		internal static IReadOnlyList<Entry> GetEntries()
		{
			if (_entries.RemoveAll(entry => !entry.TryGetChart(out _)) > 0)
			{
				Changed?.Invoke();
			}

			return _entries;
		}

		/// <summary>Adds <paramref name="chart"/> and starts recording its traces.</summary>
		internal static void Register(IStatechart chart)
		{
			var entry = new Entry(chart);

			chart.Traced += entry.Record;
			_entries.RemoveAll(e => !e.TryGetChart(out _));
			_entries.Add(entry);
			Changed?.Invoke();
		}

		/// <summary>Removes <paramref name="chart"/> and any collected entries.</summary>
		internal static void Unregister(IStatechart chart)
		{
			_entries.RemoveAll(e => !e.TryGetChart(out var c) || ReferenceEquals(c, chart));
			Changed?.Invoke();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnPlay()
		{
			// Keeps entries from a previous play session out when domain reload is disabled.
			_entries.Clear();
			Changed?.Invoke();
		}
	}
}
#endif
