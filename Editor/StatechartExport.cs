using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor
{
	/// <summary>
	/// Text exports of a statechart's topology and active states, for the Statechart Debugger's Copy menu.
	/// </summary>
	internal static class StatechartExport
	{
		/// <summary>
		/// Returns a Mermaid <c>stateDiagram-v2</c> of the chart with its active states classed <c>active</c>.
		/// </summary>
		internal static string ToMermaid(IStatechart statechart)
		{
			if (statechart == null)
			{
				throw new ArgumentNullException(nameof(statechart));
			}

			var builder = new StringBuilder();
			var active = new List<IStateNode>();

			statechart.GetActiveStates(active);
			builder.AppendLine("stateDiagram-v2");
			AppendRegion(builder, statechart.RootRegion, 1, new List<ITransitionEdge>());
			builder.AppendLine("  classDef active fill:#9be7b0,stroke:#1d7a3d,stroke-width:2px");

			var activeIds = new List<string>();

			foreach (var state in active)
			{
				if (state.Kind != StateKind.Initial && state.Kind != StateKind.Final)
				{
					activeIds.Add(Id(state));
				}
			}

			if (activeIds.Count > 0)
			{
				builder.Append("  class ").Append(string.Join(",", activeIds)).AppendLine(" active");
			}

			return builder.ToString();
		}

		/// <summary>Returns the active state paths of the chart, one per line, parents before children.</summary>
		internal static string ToActivePaths(IStatechart statechart)
		{
			if (statechart == null)
			{
				throw new ArgumentNullException(nameof(statechart));
			}

			var active = new List<IStateNode>();
			var builder = new StringBuilder();

			statechart.GetActiveStates(active);

			foreach (var state in active)
			{
				builder.AppendLine(state.Path);
			}

			return builder.ToString();
		}

		private static void AppendRegion(StringBuilder builder, IRegionNode region, int depth,
		                                 List<ITransitionEdge> leaveEdges)
		{
			var indent = new string(' ', depth * 2);

			foreach (var state in region.States)
			{
				if (state.Kind == StateKind.Initial || state.Kind == StateKind.Final)
				{
					continue;
				}

				if (state.Regions.Count == 0)
				{
					builder.Append(indent).Append(Id(state)).Append(" : ").AppendLine(Label(state));
					continue;
				}

				builder.Append(indent).Append("state \"").Append(Label(state)).Append("\" as ").Append(Id(state))
					.AppendLine(" {");

				var innerLeaves = new List<ITransitionEdge>();

				for (var i = 0; i < state.Regions.Count; i++)
				{
					if (i > 0)
					{
						builder.Append(indent).AppendLine("  --");
					}

					AppendRegion(builder, state.Regions[i], depth + 1, innerLeaves);
				}

				builder.Append(indent).AppendLine("}");

				// Mermaid cannot draw an edge out of a composite from inside it, so a leave is drawn from its owner.
				foreach (var leave in innerLeaves)
				{
					builder.Append(indent).Append(Id(state)).Append(" --> ").Append(Endpoint(leave.Target))
						.Append(" : leave ").AppendLine(leave.Source.Name.Replace(":", " "));
				}
			}

			foreach (var state in region.States)
			{
				foreach (var edge in state.Transitions)
				{
					if (edge.Target == null)
					{
						continue;
					}

					if (edge.Kind == TransitionKind.Leave)
					{
						leaveEdges.Add(edge);
						continue;
					}

					var label = edge.Event?.Name ?? (edge.HasCondition ? "[condition]" : null);

					builder.Append(indent).Append(Endpoint(edge.Source)).Append(" --> ").Append(Endpoint(edge.Target));

					if (label != null)
					{
						builder.Append(" : ").Append(label.Replace(":", " "));
					}

					builder.AppendLine();
				}
			}
		}

		private static string Endpoint(IStateNode state)
		{
			return state.Kind == StateKind.Initial || state.Kind == StateKind.Final ? "[*]" : Id(state);
		}

		private static string Id(IStateNode state)
		{
			return $"s{state.Id}";
		}

		private static string Label(IStateNode state)
		{
			var name = state.Name.Replace("\"", "'").Replace(":", " ");

			return state.Kind == StateKind.Simple || state.Kind == StateKind.Nest || state.Kind == StateKind.Split
				? name
				: $"{name} ({state.Kind})";
		}
	}
}
