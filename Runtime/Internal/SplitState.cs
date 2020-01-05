using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc cref="ISplitState"/>
	internal class SplitState : StateInternal, ISplitState
	{
		private ITransitionInternal _transition;
		private IStateInternal _initialInnerState1;
		private IStateInternal _initialInnerState2;
		private IStateInternal _currentInnerState1;
		private IStateInternal _currentInnerState2;
		
		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly IDictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public SplitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_currentInnerState1 = _initialInnerState1;
			_currentInnerState2 = _initialInnerState2;

			foreach (var action in _onEnter)
			{
				action();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			foreach (var action in _onExit)
			{
				action();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_initialInnerState1 == null || _initialInnerState2 == null)
			{
				throw new MissingMemberException($"Nest state {Name} doesn't have the nested setup defined correctly");
			}

			if (_transition.TargetState.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}

			foreach (var eventTransition in _events)
			{
				if (eventTransition.Value.TargetState.Id == Id)
				{
					throw new InvalidOperationException($"The state {Name} with the event {eventTransition.Key.Name} is pointing to itself on transition");
				}
			}
#endif
		}

		/// <inheritdoc />
		public void OnEnter(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null OnEnter action");
			}

			_onEnter.Add(action);
		}

		/// <inheritdoc />
		public void OnExit(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null OnExit action");
			}

			_onExit.Add(action);
		}

		/// <inheritdoc />
		public ITransition Event(IStatechartEvent statechartEvent)
		{
			if (statechartEvent == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null event");
			}

			var transition = new Transition();

			_events.Add(statechartEvent, transition);

			return transition;
		}

		/// <inheritdoc />
		public ITransition Split(Action<IStateFactory> setup1, Action<IStateFactory> setup2)
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} is nesting multiple times");
			}

			var nestStateFactory1 = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);
			var nestStateFactory2 = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);

			setup1(nestStateFactory1);
			setup2(nestStateFactory2);

			if (nestStateFactory1.FinalState == null || nestStateFactory2.FinalState == null)
			{
				throw new MissingMemberException($"Nest state {Name} doesn't have the nested setup defined with final states");
			}

			_stateFactory.Add(nestStateFactory1.States);
			_stateFactory.Add(nestStateFactory2.States);

			_initialInnerState1 = nestStateFactory1.InitialState;
			_initialInnerState2 = nestStateFactory2.InitialState;
			_transition = new Transition();

			return _transition;
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out ITransitionInternal transition))
			{
				_currentInnerState1.Exit();
				_currentInnerState2.Exit();

				return transition;
			}

			var nextState = _currentInnerState1.Trigger(statechartEvent);
			while (nextState != null)
			{
				_currentInnerState1 = nextState;
				nextState = _currentInnerState1.Trigger(null);
			}

			nextState = _currentInnerState2.Trigger(statechartEvent);
			while (nextState != null)
			{
				_currentInnerState2 = nextState;
				nextState = _currentInnerState2.Trigger(null);
			}

			if (_currentInnerState1 is FinalState && _currentInnerState2 is FinalState)
			{
				return _transition;
			}

			if (_currentInnerState1 is LeaveState)
			{
				return ((LeaveState)_currentInnerState1).LeaveTransition;
			}
			else if(_currentInnerState2 is LeaveState state)
			{
				return state.LeaveTransition;
			}

			return null;
		}
	}
}
