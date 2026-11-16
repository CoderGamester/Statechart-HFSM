using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <summary>
	/// Describes one outgoing transition of an internal state for the public topology.
	/// </summary>
	internal readonly struct TransitionInfo
	{
		public readonly ITransitionInternal Transition;
		public readonly IStatechartEvent Event;
		public readonly TransitionKind Kind;

		public TransitionInfo(ITransitionInternal transition, IStatechartEvent statechartEvent, TransitionKind kind)
		{
			Transition = transition;
			Event = statechartEvent;
			Kind = kind;
		}
	}

	/// <summary>
	/// Builds the immutable public <see cref="RegionNode"/> topology from a validated state factory.
	/// </summary>
	internal static class StatechartTopologyBuilder
	{
		/// <summary>
		/// Builds the root region and assigns every internal state its <see cref="StateNode"/>.
		/// </summary>
		public static RegionNode Build(IStateFactoryInternal rootFactory)
		{
			var states = new List<IStateInternal>();
			var root = BuildRegion(rootFactory, null, 0, states);
			var transitions = new List<TransitionInfo>();

			foreach (var state in states)
			{
				transitions.Clear();
				state.CollectTransitions(transitions);

				foreach (var info in transitions)
				{
					if (info.Transition == null)
					{
						continue;
					}

					state.Node._transitions.Add(new TransitionEdge(state.Node, info.Transition.TargetState?.Node,
						info.Event, info.Kind, info.Transition.HasCondition));
				}
			}

			return root;
		}

		private static RegionNode BuildRegion(IStateFactoryInternal factory, StateNode owner, int index,
		                                      List<IStateInternal> allStates)
		{
			var region = new RegionNode(owner, index, (int) factory.RegionLayer);

			foreach (var state in factory.States)
			{
				if (state.Factory != factory)
				{
					continue;
				}

				var node = new StateNode(state.Id, state.Name, state.Kind, region);

				state.Node = node;
				region._states.Add(node);
				allStates.Add(state);

				if (ReferenceEquals(state, factory.InitialState))
				{
					region.Initial = node;
				}

				if (state is not SplitState split)
				{
					continue;
				}

				for (var i = 0; i < split.Regions.Count; i++)
				{
					node._regions.Add(BuildRegion(split.Regions[i].NestedFactory, node, i, allStates));
				}
			}

			return region;
		}
	}
}
