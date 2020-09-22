using System;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart
{
	/// <summary>
	/// The representation of a state in the <see cref="IStatechart"/>
	/// </summary>
	public interface IState
	{
	}

	#region Compositions

	/// <summary>
	/// Marks a <see cref="IState"/> with the capability to have a transition to a new different <see cref="IState"/>
	/// </summary>
	public interface IStateTransition : IState
	{
		/// <summary>
		/// Setups a transition between two states and it will be processed when triggered by the current state
		/// It will return the created <see cref="ITransition"/> that will triggered when the event is processed by the state chart
		/// </summary>
		ITransition Transition();
	}

	/// <summary>
	/// TMarks a <see cref="IState"/> with the capability to have an enter execution when activated
	/// </summary>
	public interface IStateEnter : IState
	{
		/// <summary>
		/// Adds the <paramref name="action"/> to be invoked when the state is activated
		/// </summary>
		void OnEnter(Action action);
	}

	/// <summary>
	/// Marks a <see cref="IState"/> with the capability to have an exit execution when deactivated
	/// </summary>
	public interface IStateExit : IState
	{
		/// <summary>
		/// Adds the <paramref name="action"/> to be invoked when the state is deactivated
		/// </summary>
		void OnExit(Action action);
	}

	/// <summary>
	/// Marks a <see cref="IState"/> with the capability to have to trigger a state transitions
	/// </summary>
	public interface IStateEvent : IState
	{
		/// <summary>
		/// Adds the <paramref name="statechartEvent"/> to the state that will trigger a transition when processed by the state chart
		/// It will return the created <see cref="ITransition"/> that will triggered when the event is processed by the state chart
		/// </summary>
		ITransition Event(IStatechartEvent statechartEvent);
	}

	#endregion

	#region States

	/// <summary>
	/// An initial pseudo state represents a starting point for a region, that is, the point from which execution when its contained,
	/// the behavior will commence when the region is entered.
	/// A <see cref="IStatechart"/> can have only one initial state in a single region, but can have more initial states in nested regions.
	/// </summary>
	public interface IInitialState : IStateExit, IStateTransition
	{
	}

	/// <summary>
	/// A final state marks the enclosing region has completed.
	/// A Transition to a final state represents the completion of the region that contains the final state.
	/// A <see cref="IStatechart"/> can have only one final state in a single region, but can have more initial states in nested regions.
	/// </summary>
	public interface IFinalState : IStateEnter
	{
	}

	/// <summary>
	/// A simple state models a situation in the execution of the <see cref="IStatechart"/> behavior.
	/// Represents a blocking point on the state chart execution waiting for an event trigger to continue
	/// </summary>
	public interface ISimpleState : IStateEnter, IStateExit, IStateEvent
	{
	}

	/// <summary>
	/// A nest state allows the state chart to create new nested region in the <see cref="IStatechart"/>.
	/// This can be very helpfull to reduced bloated code in order to make it more readable.
	/// </summary>
	public interface INestState : IStateEnter, IStateExit, IStateEvent
	{
		/// <summary>
		/// Creates a new nested region with a specific <paramref name="setup"/>.
		/// It will return the created <see cref="ITransition"/> that will triggered as soon as the nested region is finalized.
		/// If the given <paramref name="executeExit"/> is true, it will executes the <see cref="IStateExit.OnExit"/> of
		/// the current active state when this <see cref="INestState"/> is completed.
		/// If the given <paramref name="executeFinal"/> is true, then the internal <see cref="IFinalState"/> will
		/// be executed when leaving the nested state from an event or from a <see cref="ILeaveState"/> from the nested state.
		/// </summary>
		ITransition Nest(Action<IStateFactory> setup, bool executeExit = true, bool executeFinal = false);
	}

	/// <summary>
	/// A wait state is a blocking state that holds the <see cref="IStatechart"/> behavior.
	/// It waits for the completion of defined activities or for an event to be triggered to resume the state chart execution.
	/// </summary>
	public interface IWaitState : IStateEnter, IStateExit, IStateEvent
	{
		/// <summary>
		/// Blocks the state behaviour until the given <paramref name="waitAction"/> and it's possible child activities,
		/// are completed.
		/// It will return the created <see cref="ITransition"/> that will triggered as soon as the state is unblocked
		/// </summary>
		ITransition WaitingFor(Action<IWaitActivity> waitAction);
	}

	/// <summary>
	/// A wait state is a blocking state that holds the <see cref="IStatechart"/> behavior.
	/// It waits for the completion of async defined <see cref="Task"/> to be completed to resume the state chart execution.
	/// </summary>
	public interface ITaskWaitState : IStateEnter, IStateExit
	{
		/// <summary>
		/// Blocks the state behaviour until the given async <paramref name="taskAwaitAction"/> is completed.
		/// It will return the created <see cref="ITransition"/> that will triggered as soon as the state is unblocked
		/// </summary>
		ITransition WaitingFor(Func<Task> taskAwaitAction);
	}

	/// <summary>
	/// A choice state models a situation in the execution during which some explicit condition holds the <see cref="IStatechart"/> behavior.
	/// This state doesn't block the state chart execution as it has always valid transition defined.
	/// </summary>
	public interface IChoiceState : IStateEnter, IStateExit
	{
		/// <summary>
		/// Creates a <see cref="ITransitionCondition"/> with a condition behaviour in the state
		/// It will return the created <see cref="ITransitionCondition"/> that will triggered when the condition is processed by the state chart
		/// </summary>
		ITransitionCondition Transition();
	}

	/// <summary>
	/// A nest state allows the state chart to create two new nested parallel region in the <see cref="IStatechart"/>.
	/// The two new nested regions will run and be processed in parallel.
	/// </summary>
	public interface ISplitState : IStateEnter, IStateExit, IStateEvent
	{
		/// <summary>
		/// Splits the state into two new nested parallel regions that will be active at the same time.
		/// Setup both nested region's <paramref name="setup1"/> and <paramref name="setup2"/> to have the split properly configured.
		/// It will return the created <see cref="ITransition"/> that will triggered when both nested regions finalize.
		/// their execution by both regions reaching their respectively <see cref="IFinalState"/>.
		/// </summary>
		/// <remarks>
		/// It executes the <see cref="IStateExit.OnExit"/> of the current active states when this
		/// <see cref="ISplitState"/> is completed.
		/// It does not execute the <see cref="IFinalState"/> of it's nested states when this
		/// <see cref="ISplitState"/> is completed.
		/// </remarks>
		ITransition Split(Action<IStateFactory> setup1, Action<IStateFactory> setup2);
		
		/// <inheritdoc cref="Split"/>
		/// <remarks>
		/// It executes the <see cref="IStateExit.OnExit"/> of the current active states when this
		/// <see cref="ISplitState"/> is completed.
		/// If the given <paramref name="executeFinal1"/> or <paramref name="executeFinal2"/> or is true,
		/// then the internal <see cref="IFinalState"/> will be executed when leaving the nested state from an event
		/// or from a <see cref="ILeaveState"/> from one of the inner states.
		/// </remarks>
		ITransition SplitFinal(Action<IStateFactory> setup1, Action<IStateFactory> setup2, bool executeFinal1, bool executeFinal2);
		
		/// <inheritdoc cref="Split"/>
		/// <remarks>
		/// If the given <paramref name="executeExit1"/> or <paramref name="executeExit2"/> or is true,
		/// then the internal current active <see cref="IStateExit.OnExit"/> will be executed when leaving the nested state
		/// from completion of this <see cref="ISplitState"/>.
		/// If the given <paramref name="executeFinal1"/> or <paramref name="executeFinal2"/> or is true,
		/// then the internal <see cref="IFinalState"/> will be executed when leaving the nested state from an event
		/// or from a <see cref="ILeaveState"/> from one of the inner states.
		/// </remarks>
		ITransition SplitExitFinal(Action<IStateFactory> setup1, Action<IStateFactory> setup2,
		                           bool executeExit1, bool executeExit2,
		                           bool executeFinal1, bool executeFinal2);
	}

	/// <summary>
	/// A leave state is very similar to the <see cref="IFinalState"/> and also marks the enclosing region has completed.
	/// The key difference is that the leave state transition will target a state from a different region,
	/// bypassing the <see cref="INestState"/> or the <see cref="ISplitState"/> transition target.
	/// This state must be inside of a <see cref="INestState"/> or a <see cref="ISplitState"/> and can only jump one region layer
	/// </summary>
	public interface ILeaveState : IStateEnter, IStateTransition
	{
	}

	#endregion
}