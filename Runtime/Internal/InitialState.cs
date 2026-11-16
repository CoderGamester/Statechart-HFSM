using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <inheritdoc cref="IInitialState"/>
	internal class InitialState : StateInternal, IInitialState
	{
		private ITransitionInternal _transition;

		private readonly IList<Action> _onExit = new List<Action>();

		public InitialState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
		}

		/// <inheritdoc />
		public override StateKind Kind => StateKind.Initial;

		/// <inheritdoc />
		public override void Enter()
		{
			// Do nothing on the initial state
		}

		/// <inheritdoc />
		public override void Exit()
		{
			for(int i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_transition?.TargetState == null)
			{
				throw new InvalidOperationException($"Initial state {Name} doesn't have a transition defined, so nothing will happen");
			}

			if (_transition.TargetState.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}
#endif
		}

		/// <inheritdoc />
		public ITransition Transition()
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} already has a transition defined");
			}

			_transition = new Transition();

			return _transition;
		}

		/// <inheritdoc />
		public void OnExit(Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action), $"The state {Name} cannot have a null OnExit action");
			}

			_onExit.Add(action);
		}

		/// <inheritdoc />
		public override void CollectTransitions(List<TransitionInfo> results)
		{
			results.Add(new TransitionInfo(_transition, null, TransitionKind.Automatic));
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			return _transition;
		}
	}
}