using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc cref="ISplitState"/>
	internal class SplitState : StateInternal, ISplitState
	{
		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly IDictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();
		
		private ITransitionInternal _transition;
		private IStateInternal _initialInnerState1;
		private IStateInternal _initialInnerState2;
		private IStateInternal _currentInnerState1;
		private IStateInternal _currentInnerState2;
		private IStateFactoryInternal _nestStateFactory1;
		private IStateFactoryInternal _nestStateFactory2;
		private bool _executeFinal1;
		private bool _executeFinal2;

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
			_currentInnerState1.Exit();
			_currentInnerState2.Exit();
			
			if (_executeFinal1 && !(_currentInnerState1 is FinalState) && !(_currentInnerState1 is LeaveState))
			{
				_nestStateFactory1.FinalState?.Enter();
			}
			
			if (_executeFinal2 && !(_currentInnerState2 is FinalState) && !(_currentInnerState2 is LeaveState))
			{
				_nestStateFactory2.FinalState?.Enter();
			}
			
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
				throw new MissingMemberException($"Split state {Name} doesn't have the nested setup defined correctly");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}

			foreach (var eventTransition in _events)
			{
				if (eventTransition.Value.TargetState?.Id == Id)
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
		public ITransition Split(Action<IStateFactory> setup1, Action<IStateFactory> setup2, bool executeFinal1 = false,
		                         bool executeFinal2 = false)
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} is nesting multiple times");
			}

			_nestStateFactory1 = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);
			_nestStateFactory2 = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);

			setup1(_nestStateFactory1);
			setup2(_nestStateFactory2);

			_stateFactory.Add(_nestStateFactory1.States);
			_stateFactory.Add(_nestStateFactory2.States);

			_executeFinal1 = executeFinal1;
			_executeFinal2 = executeFinal2;
			_initialInnerState1 = _nestStateFactory1.InitialState;
			_initialInnerState2 = _nestStateFactory2.InitialState;
			_transition = new Transition();

			return _transition;
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out var transition))
			{
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

			if (_currentInnerState1 is LeaveState leaveState)
			{
				return leaveState.LeaveTransition;
			}
			
			if(_currentInnerState2 is LeaveState state)
			{
				return state.LeaveTransition;
			}

			return null;
		}
	}
}
