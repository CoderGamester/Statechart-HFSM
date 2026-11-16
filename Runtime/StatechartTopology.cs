using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine
{
	/// <summary>
	/// The kind of state a <see cref="StateNode"/> describes, matching the <see cref="IStateFactory"/> method that
	/// created it.
	/// </summary>
	public enum StateKind
	{
		Initial,    // Created by IStateFactory.Initial
		Final,      // Created by IStateFactory.Final
		Simple,     // Created by IStateFactory.State
		Transition, // Created by IStateFactory.Transition
		Choice,     // Created by IStateFactory.Choice
		Wait,       // Created by IStateFactory.Wait
		TaskWait,   // Created by IStateFactory.TaskWait
		Nest,       // Created by IStateFactory.Nest
		Split,      // Created by IStateFactory.Split
		Leave       // Created by IStateFactory.Leave
	}

	/// <summary>
	/// What causes a <see cref="TransitionEdge"/> to be taken.
	/// </summary>
	public enum TransitionKind
	{
		Event,      // Taken when its event is triggered
		Automatic,  // Taken as soon as the source state is processed (initial and transition states)
		Choice,     // A choice branch, conditional or the fallback
		Completion, // Taken when the source's wait, task or nested regions complete
		Leave       // Leaves the enclosing region from a leave state
	}

	/// <summary>
	/// Read-only description of one state in a <see cref="IStatechart"/>.
	/// The topology is built once when the statechart is constructed and never changes afterwards.
	/// </summary>
	public sealed class StateNode
	{
		/// <summary>
		/// Unique id of the state within the running application.
		/// </summary>
		public uint Id { get; }
		/// <summary>
		/// The name given to the state in the setup.
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// The kind of state.
		/// </summary>
		public StateKind Kind { get; }
		/// <summary>
		/// The region that contains this state.
		/// </summary>
		public RegionNode Region { get; }
		/// <summary>
		/// The nest or split state that owns <see cref="Region"/>, or null for root states.
		/// </summary>
		public StateNode Parent => Region.Owner;
		/// <summary>
		/// The nesting depth; root states have depth 0.
		/// </summary>
		public int Depth => Region.Depth;
		/// <summary>
		/// The slash-separated names from the root to this state, for example <c>Gameplay/Idle</c>.
		/// </summary>
		public string Path { get; }
		/// <summary>
		/// The nested regions of a nest (one) or split (two or more) state; empty for every other kind.
		/// </summary>
		public IReadOnlyList<RegionNode> Regions => _regions;
		/// <summary>
		/// The outgoing transitions of this state.
		/// </summary>
		public IReadOnlyList<TransitionEdge> Transitions => _transitions;

		internal readonly List<RegionNode> _regions = new List<RegionNode>();
		internal readonly List<TransitionEdge> _transitions = new List<TransitionEdge>();

		internal StateNode(uint id, string name, StateKind kind, RegionNode region)
		{
			Id = id;
			Name = name;
			Kind = kind;
			Region = region;
			Path = region.Owner == null ? name : $"{region.Owner.Path}/{name}";
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Path;
		}
	}

	/// <summary>
	/// Read-only description of one region: a sequential group of states with a single initial state.
	/// </summary>
	public sealed class RegionNode
	{
		/// <summary>
		/// The nest or split state that owns this region, or null for the root region.
		/// </summary>
		public StateNode Owner { get; }
		/// <summary>
		/// The position of this region among its owner's regions.
		/// </summary>
		public int Index { get; }
		/// <summary>
		/// The nesting depth of the states in this region; the root region has depth 0.
		/// </summary>
		public int Depth { get; }
		/// <summary>
		/// The states declared in this region, in declaration order.
		/// </summary>
		public IReadOnlyList<StateNode> States => _states;
		/// <summary>
		/// The initial state of this region.
		/// </summary>
		public StateNode Initial { get; internal set; }

		internal readonly List<StateNode> _states = new List<StateNode>();

		internal RegionNode(StateNode owner, int index, int depth)
		{
			Owner = owner;
			Index = index;
			Depth = depth;
		}
	}

	/// <summary>
	/// Read-only description of one transition between two states.
	/// </summary>
	public sealed class TransitionEdge
	{
		/// <summary>
		/// The state that owns the transition.
		/// </summary>
		public StateNode Source { get; }
		/// <summary>
		/// The state the transition moves to, or null for an action-only event transition.
		/// </summary>
		public StateNode Target { get; }
		/// <summary>
		/// The event that triggers the transition, or null when <see cref="Kind"/> is not
		/// <see cref="TransitionKind.Event"/>.
		/// </summary>
		public IStatechartEvent Event { get; }
		/// <summary>
		/// What causes the transition.
		/// </summary>
		public TransitionKind Kind { get; }
		/// <summary>
		/// True when the transition has at least one guard condition.
		/// </summary>
		public bool HasCondition { get; }

		internal TransitionEdge(StateNode source, StateNode target, IStatechartEvent statechartEvent,
		                        TransitionKind kind, bool hasCondition)
		{
			Source = source;
			Target = target;
			Event = statechartEvent;
			Kind = kind;
			HasCondition = hasCondition;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var label = Event?.Name ?? Kind.ToString();

			return $"{Source.Name} -({label})-> {Target?.Name ?? "(action only)"}";
		}
	}
}
