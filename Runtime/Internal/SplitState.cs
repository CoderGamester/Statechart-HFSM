using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <inheritdoc cref="ISplitState"/>
	internal class SplitState : StateInternal, ISplitState
	{
		protected readonly IList<Action> _onEnter = new List<Action>();
		protected readonly IList<Action> _onExit = new List<Action>();
		protected readonly IDictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();
		protected readonly IList<InnerStateData> _innerStatesData = new List<InnerStateData>();

		private bool _isPaused;
		private ITransitionInternal _transition;

		public SplitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_isPaused = false;

			for (var i = 0; i < _innerStatesData.Count; i++)
			{
				var innerState = _innerStatesData[i];

				innerState.CurrenState = innerState.InitialState;

				_innerStatesData[i] = innerState;
			}

			foreach (var action in _onEnter)
			{
				action();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			_isPaused = false;

			for (var i = 0; i < _innerStatesData.Count; i++)
			{
				var innerState = _innerStatesData[i];

				if (innerState.ExecuteExit)
				{
					innerState.CurrenState.Exit();
				}

				if (innerState.ExecuteFinal && !(innerState.CurrenState is FinalState) && !(innerState.CurrenState is LeaveState))
				{
					innerState.NestedFactory.FinalState?.Enter();
				}
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
			if (_innerStatesData.Count < 2)
			{
				throw new InvalidOperationException($"Split state {Name} doesn't have enough nested setup defined." +
				                                 $"It needs min 2 nested states to be a proper {nameof(ISplitState)}");
			}
#endif
			OnValidate();
		}

		protected void OnValidate()
		{
#if UNITY_EDITOR || DEBUG
			if (_innerStatesData.Count == 0)
			{
				throw new InvalidOperationException($"This state {Name} doesn't have the nested setup defined correctly");
			}
			
			for (var i = 0; i < _innerStatesData.Count; i++)
			{
				foreach(var state in _innerStatesData[i].NestedFactory.States)
				{
					state.Validate();
				}
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
		public ITransition Split(params Action<IStateFactory>[] data)
		{
			var array = new NestedStateData[data.Length];

			for (var i = 0; i < array.Length; i++)
			{
				array[i] = data[i];
			}

			return Split(array);
		}

		/// <inheritdoc />
		public ITransition Split(params NestedStateData[] data)
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} is nesting multiple times");
			}

			foreach (var stateData in data)
			{
				var factory = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);
				
				stateData.Setup(factory);
				
				_stateFactory.Add(factory.States);
				_innerStatesData.Add(new InnerStateData
				{
					InitialState = factory.InitialState,
					CurrenState = null,
					NestedFactory = factory,
					ExecuteExit = stateData.ExecuteExit,
					ExecuteFinal = stateData.ExecuteFinal
				});
			}
			
			_transition = new Transition();

			return _transition;
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if(_isPaused && !IsAllCompleted())
			{
				return null;
			}

			var isUnpausing = _isPaused;
			_isPaused = false;

			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out var transition))
			{
				// Delay the completion of this state while some tasks are running
				return !isUnpausing && DelayForceComplete(statechartEvent) ? null : transition;
			}

			return ProcessInnerStates(statechartEvent, isUnpausing);
		}

		/// <summary>
		/// This call allows to delay the comletion of this state for when any awatable tasks block it's completiong.
		/// In the meantime will forcely complete any <see cref="WaitState"/> or queue <see cref="IStatechartEvent"/>
		/// for any <see cref="TaskWaitState"/> are running to be completed.
		/// It will pause this state until all the tasks are completed
		/// This method takes care of nested states in order to avoid miss connection with it's setup.
		/// Returns true if the state is paused for the completion of inner <see cref="TaskWaitState"/>
		/// </summary>
		internal bool DelayForceComplete(IStatechartEvent statechartEvent)
		{
			_isPaused = false;

			for (var i = 0; i < _innerStatesData.Count; i++)
			{
				var currenState = _innerStatesData[i].CurrenState;

				if(currenState is WaitState waitState)
				{
					waitState.ForceComplete();
				}
				else if(currenState is TaskWaitState taskState && !taskState.Completed)
				{
					_isPaused = true;

					taskState.EnqueuEvent(statechartEvent);
				}
				else if(currenState is SplitState splitState)
				{
					_isPaused = _isPaused || splitState.DelayForceComplete(statechartEvent);
				}
			}

			return _isPaused;
		}
		
		/// <summary>
		/// Checks if all inner states on hold are already completed
		/// </summary>
		internal bool IsAllCompleted()
		{
			for (var i = 0; i < _innerStatesData.Count; i++)
			{
				var currenState = _innerStatesData[i].CurrenState;

				if (currenState is TaskWaitState taskState && !taskState.Completed)
				{
					return false;
				}
				else if (currenState is SplitState splitState && !splitState.IsAllCompleted())
				{
					return false;
				}
			}

			return true;
		}

		private ITransitionInternal ProcessInnerStates(IStatechartEvent statechartEvent, bool isUnpausing)
		{
			var nextTransition = _transition;
			var leaveState = (LeaveState) null;

			for (var i = 0; i < _innerStatesData.Count; i++)
			{
				var innerState = _innerStatesData[i];

				if(!isUnpausing)
				{
					var nextState = innerState.CurrenState.Trigger(statechartEvent);

					while (nextState != null)
					{
						innerState.CurrenState = nextState;
						nextState = innerState.CurrenState.Trigger(null);
					}
				}

				if (innerState.CurrenState is LeaveState state)
				{
					leaveState = state;
				}
				else if (innerState.CurrenState is not FinalState)
				{
					nextTransition = null;
				}

				_innerStatesData[i] = innerState;
			}

			if(leaveState != null)
			{
				// Delay the completion of this state while some tasks are running
				nextTransition = !isUnpausing && DelayForceComplete(null) ? null : leaveState.LeaveTransition;
			}

			return nextTransition;
		}
	}
}