using System.Collections.Generic;
using GameLovers.StatechartMachine.Internal;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor
{
	/// <summary>
	/// View A: the chart drawn as nested boxes. Nest and split states contain their regions, split regions sit side
	/// by side as dashed lanes, and every active state is highlighted.
	/// </summary>
	internal sealed class LiveChartView : VisualElement
	{
		private const double RecentSeconds = 0.8;

		private readonly Dictionary<StateNode, VisualElement> _elements = new Dictionary<StateNode, VisualElement>();
		private readonly ScrollView _scroll;

		internal LiveChartView()
		{
			AddToClassList("sc-pane");
			AddToClassList("sc-pane-live");

			var header = new Label("A · Live chart");
			header.AddToClassList("sc-pane-header");
			Add(header);

			// Vertical only: a bounded width lets each region's boxes wrap onto new rows.
			_scroll = new ScrollView(ScrollViewMode.Vertical);
			_scroll.AddToClassList("sc-pane-content");
			Add(_scroll);
		}

		/// <summary>Rebuilds the boxes for <paramref name="chart"/>, or clears them when null.</summary>
		internal void SetChart(IStatechart chart)
		{
			_elements.Clear();
			_scroll.Clear();

			if (chart != null)
			{
				_scroll.Add(BuildRegion(chart.RootRegion, false));
			}
		}

		/// <summary>Highlights <paramref name="active"/> states and briefly flashes recently entered ones.</summary>
		internal void UpdateActive(List<StateNode> active, IReadOnlyCollection<StatechartTraceRecord> history)
		{
			var recent = new HashSet<StateNode>();
			var now = Time.realtimeSinceStartupAsDouble;

			foreach (var record in history)
			{
				if (record.Kind == StatechartTraceKind.StateEntered && now - record.Time < RecentSeconds)
				{
					recent.Add(record.State);
				}
			}

			foreach (var pair in _elements)
			{
				pair.Value.EnableInClassList("sc-active", active.Contains(pair.Key));
				pair.Value.EnableInClassList("sc-recent", recent.Contains(pair.Key));
			}
		}

		private VisualElement BuildRegion(RegionNode region, bool isLane)
		{
			var container = new VisualElement();

			container.AddToClassList("sc-region");
			container.EnableInClassList("sc-lane", isLane);

			foreach (var state in region.States)
			{
				container.Add(BuildState(state));
			}

			return container;
		}

		private VisualElement BuildState(StateNode state)
		{
			var box = new VisualElement { tooltip = $"{state.Path}\n{state.Kind}" };

			box.AddToClassList("sc-state");
			box.AddToClassList($"sc-kind-{state.Kind.ToString().ToLowerInvariant()}");
			_elements[state] = box;

			if (state.Kind == StateKind.Initial || state.Kind == StateKind.Final)
			{
				box.AddToClassList("sc-pseudo");
				box.Add(new Label(state.Kind == StateKind.Initial ? "●" : "◉"));
				return box;
			}

			var title = new VisualElement();
			title.AddToClassList("sc-state-title");
			title.Add(new Label(state.Name) { name = "name" });

			if (state.Kind != StateKind.Simple)
			{
				var badge = new Label(state.Kind.ToString());
				badge.AddToClassList("sc-badge");
				title.Add(badge);
			}

			box.Add(title);

			if (state.Regions.Count > 0)
			{
				var regions = new VisualElement();

				regions.AddToClassList("sc-regions");
				regions.EnableInClassList("sc-regions-split", state.Kind == StateKind.Split);

				foreach (var region in state.Regions)
				{
					regions.Add(BuildRegion(region, state.Kind == StateKind.Split));
				}

				box.Add(regions);
			}

			foreach (var edge in state.Transitions)
			{
				if (edge.Kind == TransitionKind.Automatic && edge.Target != null)
				{
					continue;
				}

				var chip = new Label($"{EdgeLabel(edge)} → {edge.Target?.Name ?? "action"}");
				chip.AddToClassList("sc-edge");
				box.Add(chip);
			}

			return box;
		}

		private static string EdgeLabel(TransitionEdge edge)
		{
			switch (edge.Kind)
			{
				case TransitionKind.Event:
					return edge.Event.Name;
				case TransitionKind.Choice:
					return edge.HasCondition ? "if" : "else";
				case TransitionKind.Completion:
					return "done";
				case TransitionKind.Leave:
					return "leave";
				default:
					return "auto";
			}
		}
	}
}
