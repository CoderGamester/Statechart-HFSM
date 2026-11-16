using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine
{
	/// <summary>
	/// Text exports of a statechart's topology and active states, for logs, bug reports and AI agents.
	/// </summary>
	public static class StatechartExport
	{
		/// <summary>
		/// Returns a Mermaid <c>stateDiagram-v2</c> of <paramref name="statechart"/>, with active states marked by the
		/// <c>active</c> class. Initial and final states map to Mermaid's <c>[*]</c> markers.
		/// </summary>
		/// <param name="statechart">The chart to export.</param>
		/// <returns>The Mermaid source text.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="statechart"/> is null.</exception>
		public static string ToMermaid(IStatechart statechart)
		{
			if (statechart == null)
			{
				throw new ArgumentNullException(nameof(statechart));
			}

			var builder = new StringBuilder();
			var active = new List<StateNode>();

			statechart.GetActiveStates(active);
			builder.AppendLine("stateDiagram-v2");
			AppendRegion(builder, statechart.RootRegion, 1, new List<TransitionEdge>());
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

		/// <summary>
		/// Returns the active states of <paramref name="statechart"/> as one path per line, for example
		/// <c>Gameplay/Player/Idle</c>, parents before children.
		/// </summary>
		/// <param name="statechart">The chart to describe.</param>
		/// <returns>The active state paths.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="statechart"/> is null.</exception>
		public static string ToActivePaths(IStatechart statechart)
		{
			if (statechart == null)
			{
				throw new ArgumentNullException(nameof(statechart));
			}

			var active = new List<StateNode>();
			var builder = new StringBuilder();

			statechart.GetActiveStates(active);

			foreach (var state in active)
			{
				builder.AppendLine(state.Path);
			}

			return builder.ToString();
		}

		private static void AppendRegion(StringBuilder builder, RegionNode region, int depth, List<TransitionEdge> leaveEdges)
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

				var innerLeaves = new List<TransitionEdge>();

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

		private static string Endpoint(StateNode state)
		{
			return state.Kind == StateKind.Initial || state.Kind == StateKind.Final ? "[*]" : Id(state);
		}

		private static string Id(StateNode state)
		{
			return $"s{state.Id}";
		}

		private static string Label(StateNode state)
		{
			var name = state.Name.Replace("\"", "'").Replace(":", " ");

			return state.Kind == StateKind.Simple || state.Kind == StateKind.Nest || state.Kind == StateKind.Split
				? name
				: $"{name} ({state.Kind})";
		}
	}
}
