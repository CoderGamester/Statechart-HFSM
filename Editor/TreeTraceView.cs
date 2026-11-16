using System.Collections.Generic;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor
{
	/// <summary>
	/// View C: an indented tree of every state with the active ones marked, above a scrolling trace log.
	/// </summary>
	internal sealed class TreeTraceView : VisualElement
	{
		private readonly Dictionary<IStateNode, Label> _rows = new Dictionary<IStateNode, Label>();
		private readonly List<StatechartTraceRecord> _records = new List<StatechartTraceRecord>();
		private readonly ScrollView _tree;
		private readonly ListView _trace;

		internal TreeTraceView()
		{
			AddToClassList("sc-pane");
			AddToClassList("sc-pane-tree");

			var treeHeader = new Label("C · State tree");
			treeHeader.AddToClassList("sc-pane-header");
			Add(treeHeader);

			_tree = new ScrollView();
			_tree.AddToClassList("sc-tree");
			Add(_tree);

			var traceHeader = new Label("Trace (newest last)");
			traceHeader.AddToClassList("sc-pane-header");
			Add(traceHeader);

			_trace = new ListView(_records, 18, MakeTraceRow, BindTraceRow)
			{
				selectionType = SelectionType.None
			};
			_trace.AddToClassList("sc-trace");
			Add(_trace);
		}

		/// <summary>Rebuilds the tree for <paramref name="chart"/>, or clears it when null.</summary>
		internal void SetChart(IStatechart chart)
		{
			_rows.Clear();
			_tree.Clear();
			_records.Clear();
			_trace.RefreshItems();

			if (chart != null)
			{
				AddRegion(chart.RootRegion);
			}
		}

		/// <summary>Marks <paramref name="active"/> states and shows the latest <paramref name="history"/>.</summary>
		internal void UpdateActive(List<IStateNode> active, IReadOnlyCollection<StatechartTraceRecord> history)
		{
			foreach (var pair in _rows)
			{
				var isActive = active.Contains(pair.Key);

				pair.Value.text = $"{(isActive ? "●" : "○")} {pair.Key.Name}";
				pair.Value.EnableInClassList("sc-active", isActive);
			}

			_records.Clear();
			_records.AddRange(history);
			_trace.RefreshItems();

			if (_records.Count > 0)
			{
				_trace.ScrollToItem(_records.Count - 1);
			}
		}

		private void AddRegion(IRegionNode region)
		{
			if (region.Owner != null && region.Owner.Kind == StateKind.Split)
			{
				var lane = new Label($"region {region.Index + 1}");
				lane.AddToClassList("sc-tree-lane");
				lane.style.paddingLeft = region.Depth * 14;
				_tree.Add(lane);
			}

			foreach (var state in region.States)
			{
				var row = new Label { tooltip = $"{state.Path} ({state.Kind})" };

				row.AddToClassList("sc-tree-row");
				row.style.paddingLeft = region.Depth * 14 + 4;
				_rows[state] = row;
				_tree.Add(row);

				foreach (var child in state.Regions)
				{
					AddRegion(child);
				}
			}
		}

		private static VisualElement MakeTraceRow()
		{
			var label = new Label();

			label.AddToClassList("sc-trace-row");

			return label;
		}

		private void BindTraceRow(VisualElement element, int index)
		{
			var record = _records[index];
			var label = (Label) element;

			label.text = $"#{record.Frame}  {Describe(record)}";
			label.EnableInClassList("sc-trace-event", record.Kind == StatechartTraceKind.EventTriggered);
			label.EnableInClassList("sc-trace-fault", record.Kind == StatechartTraceKind.Faulted);
			label.EnableInClassList("sc-trace-enter", record.Kind == StatechartTraceKind.StateEntered);
		}

		private static string Describe(StatechartTraceRecord record)
		{
			switch (record.Kind)
			{
				case StatechartTraceKind.EventTriggered:
					return $"⚡ {record.EventName}";
				case StatechartTraceKind.StateEntered:
					return $"→ enter {record.StatePath}";
				case StatechartTraceKind.StateExited:
					return $"← exit {record.StatePath}";
				case StatechartTraceKind.Transition:
					return $"   {record.StatePath} ⟶ {record.TargetPath ?? "(action)"}";
				case StatechartTraceKind.Faulted:
					return $"✖ fault in {record.StatePath}";
				default:
					return record.Kind.ToString();
			}
		}
	}
}
