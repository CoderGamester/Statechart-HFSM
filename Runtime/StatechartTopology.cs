using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine
{
	/// <summary>
	/// The kind of state an <see cref="IStateNode"/> describes, matching the <see cref="IStateFactory"/> method that
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
	/// What causes an <see cref="ITransitionEdge"/> to be taken.
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
	/// Read-only view of one state in a <see cref="IStatechart"/>.
	/// It is the live state itself, so the description never goes stale and costs nothing extra to build.
	/// </summary>
	public interface IStateNode
	{
		/// <summary>
		/// Unique id of the state within the running application.
		/// </summary>
		uint Id { get; }
		/// <summary>
		/// The name given to the state in the setup.
		/// </summary>
		string Name { get; }
		/// <summary>
		/// The kind of state.
		/// </summary>
		StateKind Kind { get; }
		/// <summary>
		/// The region that contains this state.
		/// </summary>
		IRegionNode Region { get; }
		/// <summary>
		/// The slash-separated names from the root to this state, for example <c>Gameplay/Idle</c>.
		/// </summary>
		string Path { get; }
		/// <summary>
		/// The nested regions of a nest (one) or split (two or more) state; empty for every other kind.
		/// </summary>
		IReadOnlyList<IRegionNode> Regions { get; }
		/// <summary>
		/// The outgoing transitions of this state, in declaration order.
		/// </summary>
		IReadOnlyList<ITransitionEdge> Transitions { get; }
	}

	/// <summary>
	/// Read-only view of one region: a sequential group of states with a single initial state.
	/// </summary>
	public interface IRegionNode
	{
		/// <summary>
		/// The nest or split state that owns this region, or null for the root region.
		/// </summary>
		IStateNode Owner { get; }
		/// <summary>
		/// The position of this region among its owner's regions.
		/// </summary>
		int Index { get; }
		/// <summary>
		/// The nesting depth of the states in this region; the root region has depth 0.
		/// </summary>
		int Depth { get; }
		/// <summary>
		/// The states declared in this region, in declaration order.
		/// </summary>
		IReadOnlyList<IStateNode> States { get; }
		/// <summary>
		/// The initial state of this region.
		/// </summary>
		IStateNode Initial { get; }
	}

	/// <summary>
	/// Read-only view of one transition between two states.
	/// </summary>
	public interface ITransitionEdge
	{
		/// <summary>
		/// The state that owns the transition.
		/// </summary>
		IStateNode Source { get; }
		/// <summary>
		/// The state the transition moves to, or null for an action-only event transition.
		/// </summary>
		IStateNode Target { get; }
		/// <summary>
		/// The event that triggers the transition, or null when <see cref="Kind"/> is not
		/// <see cref="TransitionKind.Event"/>.
		/// </summary>
		IStatechartEvent Event { get; }
		/// <summary>
		/// What causes the transition.
		/// </summary>
		TransitionKind Kind { get; }
		/// <summary>
		/// True when the transition has at least one guard condition.
		/// </summary>
		bool HasCondition { get; }
	}
}
