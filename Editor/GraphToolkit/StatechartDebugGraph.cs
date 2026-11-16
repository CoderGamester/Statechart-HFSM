using System;
using System.Linq;
using Unity.GraphToolkit.Editor;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor.GraphToolkit
{
	/// <summary>
	/// Graph Toolkit asset that mirrors a live statechart's topology for view B. It is generated from code and only
	/// used for display; editing it has no effect on the statechart.
	/// </summary>
	[Graph(AssetExtension)]
	[Serializable]
	internal class StatechartDebugGraph : Graph
	{
		internal const string AssetExtension = "statechartdebug";

		/// <summary>Reports that the graph is display only, since Graph Toolkit offers no read-only mode.</summary>
		public override void OnGraphChanged(GraphLogger logger)
		{
			// A message without a node context is not drawn in the graph window, so it is pinned to the first node.
			logger.LogWarning("Display only: rebuilt from the running chart each time B Graph opens it. " +
			                  "Edits here do not change the game.", GetNodes().FirstOrDefault());
		}
	}

	/// <summary>
	/// One statechart state. "Out" wires show transitions; dashed "Regions" wires lead to nested regions.
	/// </summary>
	[Serializable]
	internal class StatechartStateNode : Node
	{
		internal const string InPort = "In";
		internal const string OutPort = "Out";
		internal const string RegionsPort = "Regions";

		/// <summary>Declares the In, Out and Regions ports.</summary>
		protected override void OnDefinePorts(IPortDefinitionContext context)
		{
			context.AddInputPort(InPort).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
			context.AddOutputPort(OutPort).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
			context.AddOutputPort(RegionsPort).Build();
		}
	}
}
