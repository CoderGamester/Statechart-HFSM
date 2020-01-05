using System;
using System.Collections.Generic;
using GameLovers.Statechart;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc />
	internal interface ITransitionInternal : ITransitionCondition
	{
		IStateInternal TargetState { get; }
		bool HasCondition { get; }
		string CreationStackTrace { get; }
		
		bool CheckCondition();
		void TriggerTransition();
	}

	/// <inheritdoc />
	internal class Transition : ITransitionInternal
	{
		private readonly IList<Action> _onTransition = new List<Action>();
		private readonly IList<Func<bool>> _condition = new List<Func<bool>>();

		/// <inheritdoc />
		public IStateInternal TargetState { get; private set; }
		/// <inheritdoc />
		public bool HasCondition { get { return _condition.Count > 0; } }
		/// <inheritdoc />
		public string CreationStackTrace { get; private set; }

		/// <inheritdoc />
		public bool CheckCondition()
		{
			var ret = true;

			for(int i = 0; i < _condition.Count; i++)
			{
				ret = ret && _condition[i].Invoke();
			}

			return ret;
		}

		/// <inheritdoc />
		public void TriggerTransition()
		{
			for(int i = 0; i < _onTransition.Count; i++)
			{
				_onTransition[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public ITransition Condition(Func<bool> condition)
		{
			if(condition == null)
			{
				throw new NullReferenceException("The transition cannot have a null condition");
			}

#if UNITY_EDITOR || DEBUG
			CreationStackTrace = StatechartUtils.RemoveGarbageFromStackTrace(Environment.StackTrace);
#endif

			_condition.Add(condition);

			return this;
		}

		/// <inheritdoc />
		public ITransition OnTransition(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException("The transition cannot have a null OnTransition action");
			}

#if UNITY_EDITOR || DEBUG
			CreationStackTrace = StatechartUtils.RemoveGarbageFromStackTrace(Environment.StackTrace);
#endif

			_onTransition.Add(action);

			return this;
		}

		/// <inheritdoc />
		public void Target(IState state)
		{
			if (state == null)
			{
				throw new NullReferenceException("The transition cannot have a null target state");
			}

			TargetState = (IStateInternal) state;
		}
	}
}