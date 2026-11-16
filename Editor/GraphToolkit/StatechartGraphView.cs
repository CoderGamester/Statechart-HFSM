using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor.GraphToolkit
{
	/// <summary>
	/// View B: generates a Graph Toolkit graph from a live chart, opens it, and while the chart runs paints active
	/// states bright green and thickens taken transitions.
	/// </summary>
	[InitializeOnLoad]
	internal static class StatechartGraphView
	{
		internal const string AssetFolder = "Assets/StatechartDebugger";

		private const float ColumnWidth = 260f;
		private const float RowHeight = 150f;
		private const float WireWidth = 5f;
		private const float InactiveDim = 0.45f;

		private static readonly Color _activeColor = new Color(0.25f, 0.95f, 0.45f);

		private static Session _session;

		private sealed class Session
		{
			public StatechartRegistry.Entry Entry;
			public Hash128 GraphId;
			public double LastRecordTime = -1;
			public int AppliedVersion = -1;
			// Ids and kinds only: the session outlives play sessions and must not keep a chart's states alive.
			public readonly Dictionary<Hash128, (uint Id, StateKind Kind)> States =
				new Dictionary<Hash128, (uint Id, StateKind Kind)>();
			public readonly List<IStateNode> Active = new List<IStateNode>();
		}

		static StatechartGraphView()
		{
			StatechartDebuggerWindow.GraphViewOpener = (chart, entry) => Open(chart, entry);
			EditorApplication.update += Update;
		}

		/// <summary>Generates (or regenerates) the graph asset for <paramref name="chart"/> and opens it.</summary>
		internal static string Open(IStatechart chart, StatechartRegistry.Entry entry)
		{
			var path = $"{AssetFolder}/{Sanitize(chart.Name)}.{StatechartDebugGraph.AssetExtension}";

			Directory.CreateDirectory(AssetFolder);
			_session = new Session { Entry = entry };

			var graph = GraphDatabase.LoadGraph<StatechartDebugGraph>(path) ?? GraphDatabase.CreateGraph<StatechartDebugGraph>(path);

			graph.UndoBeginRecordGraph("Generate statechart graph");

			foreach (var node in graph.GetNodes().ToList())
			{
				graph.RemoveNode(node);
			}

			var nodes = new Dictionary<IStateNode, StatechartStateNode>();

			LayoutRegion(graph, chart.RootRegion, 0f, 0f, nodes);
			ConnectTransitions(graph, nodes);
			GraphDatabase.SaveGraph(graph);
			graph.UndoEndRecordGraph();

			foreach (var pair in nodes)
			{
				_session.States[pair.Value.ID] = (pair.Key.Id, pair.Key.Kind);
			}

			_session.GraphId = graph.ID;
			AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(path));

			return path;
		}

		private static float LayoutRegion(StatechartDebugGraph graph, IRegionNode region, float x, float y,
		                                  Dictionary<IStateNode, StatechartStateNode> nodes)
		{
			var column = x;

			foreach (var state in region.States)
			{
				var node = new StatechartStateNode { Position = new Vector2(column, y) };

				graph.AddNode(node);
				node.Title = state.Name;
				node.Subtitle = state.Kind.ToString();
				node.Tooltip = state.Path;
				node.DefaultColor = ColorFor(state.Kind);
				nodes[state] = node;
				column += ColumnWidth;
			}

			var nextY = y + RowHeight;

			foreach (var state in region.States)
			{
				foreach (var child in state.Regions)
				{
					nextY = LayoutRegion(graph, child, nodes[state].Position.x + ColumnWidth * 0.5f, nextY, nodes);
				}
			}

			return nextY;
		}

		private static void ConnectTransitions(StatechartDebugGraph graph,
		                                       Dictionary<IStateNode, StatechartStateNode> nodes)
		{
			foreach (var pair in nodes)
			{
				var output = pair.Value.GetOutputPortByName(StatechartStateNode.OutPort);

				foreach (var edge in pair.Key.Transitions)
				{
					if (edge.Target != null)
					{
						graph.Connect(output, nodes[edge.Target].GetInputPortByName(StatechartStateNode.InPort));
					}
				}

				var regions = pair.Value.GetOutputPortByName(StatechartStateNode.RegionsPort);

				foreach (var region in pair.Key.Regions)
				{
					if (region.Initial == null)
					{
						continue;
					}

					var input = nodes[region.Initial].GetInputPortByName(StatechartStateNode.InPort);

					graph.Connect(regions, input);
					graph.GetWire(regions, input).IsDashed = true;
				}
			}
		}

		private static void Update()
		{
			if (_session == null || _session.AppliedVersion == StatechartRegistry.Version ||
			    !_session.Entry.TryGetChart(out var chart))
			{
				return;
			}

			var graph = FindOpenGraph(_session.GraphId);

			if (graph == null)
			{
				return;
			}

			var nodes = new Dictionary<uint, StatechartStateNode>();

			chart.GetActiveStates(_session.Active);

			// The graph window redraws authoring changes made inside an undo record; GraphVisualization fills did not
			// render on Unity 6000.6, so the highlight is written to the display-only graph itself.
			graph.UndoBeginRecordGraph("Statechart Debugger highlight");

			foreach (var node in graph.GetNodes())
			{
				if (node is not StatechartStateNode stateNode ||
				    !_session.States.TryGetValue(stateNode.ID, out var state))
				{
					continue;
				}

				var color = ContainsId(_session.Active, state.Id) ? _activeColor : Dim(ColorFor(state.Kind));

				nodes[state.Id] = stateNode;

				if (stateNode.DefaultColor != color)
				{
					stateNode.DefaultColor = color;
				}
			}

			_session.Active.Clear();

			foreach (var record in _session.Entry.History)
			{
				if (record.Time <= _session.LastRecordTime)
				{
					continue;
				}

				_session.LastRecordTime = record.Time;

				if (record.Kind == StatechartTraceKind.Transition && nodes.TryGetValue(record.StateId, out var source) &&
				    nodes.TryGetValue(record.TargetId, out var target))
				{
					// Taken transitions stay thick, so the path the chart walked remains visible.
					var wire = graph.GetWire(source.GetOutputPortByName(StatechartStateNode.OutPort),
						target.GetInputPortByName(StatechartStateNode.InPort));

					if (wire != null)
					{
						wire.WidthOverride = WireWidth;
					}
				}
			}

			graph.UndoEndRecordGraph();
			_session.AppliedVersion = StatechartRegistry.Version;
		}

		private static bool ContainsId(List<IStateNode> states, uint id)
		{
			foreach (var state in states)
			{
				if (state.Id == id)
				{
					return true;
				}
			}

			return false;
		}

		private static Graph FindOpenGraph(Hash128 graphId)
		{
			foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if (window is IGraphWindow graphWindow && graphWindow.Graph != null && graphWindow.Graph.ID == graphId)
				{
					return graphWindow.Graph;
				}
			}

			return null;
		}

		private static Color ColorFor(StateKind kind)
		{
			switch (kind)
			{
				case StateKind.Initial:
				case StateKind.Final:
					return new Color(0.55f, 0.55f, 0.55f);
				case StateKind.Nest:
				case StateKind.Split:
					return new Color(0.45f, 0.4f, 0.85f);
				case StateKind.Wait:
				case StateKind.TaskWait:
					return new Color(0.9f, 0.6f, 0.2f);
				case StateKind.Choice:
				case StateKind.Transition:
				case StateKind.Leave:
					return new Color(0.2f, 0.65f, 0.75f);
				default:
					return new Color(0.24f, 0.7f, 0.37f);
			}
		}

		private static Color Dim(Color color)
		{
			return new Color(color.r * InactiveDim, color.g * InactiveDim, color.b * InactiveDim);
		}

		private static string Sanitize(string name)
		{
			foreach (var invalid in Path.GetInvalidFileNameChars())
			{
				name = name.Replace(invalid, '_');
			}

			return name;
		}
	}
}
