using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor
{
	/// <summary>
	/// A trace step copied as ids and text, so recorded history never keeps a chart or its states alive.
	/// </summary>
	internal readonly struct StatechartTraceRecord
	{
		public readonly double Time;
		public readonly int Frame;
		public readonly StatechartTraceKind Kind;
		public readonly uint StateId;
		public readonly uint TargetId;
		public readonly string StatePath;
		public readonly string TargetPath;
		public readonly string EventName;
		public readonly string Message;

		public StatechartTraceRecord(in StatechartTrace trace, double time, int frame)
		{
			Time = time;
			Frame = frame;
			Kind = trace.Kind;
			StateId = trace.State?.Id ?? 0;
			TargetId = trace.Target?.Id ?? 0;
			StatePath = trace.State?.Path;
			TargetPath = trace.Target?.Path;
			EventName = trace.Event?.Name;
			Message = trace.ToString();
		}
	}

	/// <summary>
	/// List of live statecharts and their recent trace history, read by the Statechart Debugger.
	/// Charts are held weakly, so a chart that is never disposed still disappears once it is collected.
	/// </summary>
	[InitializeOnLoad]
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

			/// <summary>Records <paramref name="trace"/>; a Disposed trace also leaves the registry.</summary>
			internal void Record(StatechartTrace trace)
			{
				if (_history.Count == HistoryCapacity)
				{
					_history.Dequeue();
				}

				_history.Enqueue(new StatechartTraceRecord(trace, Time.realtimeSinceStartupAsDouble, Time.frameCount));
				Version++;

				if (trace.Kind == StatechartTraceKind.Disposed && _entries.Remove(this))
				{
					Changed?.Invoke();
				}
			}
		}

		static StatechartRegistry()
		{
			Statechart.Created += Register;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
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

		private static void Register(Statechart chart)
		{
			var entry = new Entry(chart);

			chart.Traced += entry.Record;
			_entries.RemoveAll(e => !e.TryGetChart(out _));
			_entries.Add(entry);
			Changed?.Invoke();
		}

		private static void OnPlayModeChanged(PlayModeStateChange change)
		{
			if (change != PlayModeStateChange.ExitingEditMode)
			{
				return;
			}

			// Keeps charts from a previous play session out when domain reload is disabled.
			_entries.Clear();
			Changed?.Invoke();
		}
	}
}
