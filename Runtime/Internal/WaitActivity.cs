using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc />
	internal interface IWaitActivityInternal : IWaitActivity
	{
		uint Id { get; }
		bool IsCompleted { get; }
		void Reset();
	}

	/// <inheritdoc />
	internal class WaitActivity : IWaitActivityInternal
	{
		private static uint _idRef;

		private readonly Action<IStatechartEvent> _onComplete;
		private readonly Action<uint> _innerOnComplete;
		private readonly Dictionary<uint, bool> _activities = new Dictionary<uint, bool>();

		/// <inheritdoc />
		public uint Id { get; private set; }
		/// <inheritdoc />
		public bool IsCompleted { get; private set; }

		private WaitActivity()
		{
			Id = ++_idRef;

			_activities.Add(Id, false);
		}

		private WaitActivity(Action<uint> innerOnComplete) : this()
		{
			_innerOnComplete = innerOnComplete;
		}

		public WaitActivity(Action<IStatechartEvent> onComplete) : this()
		{
			_onComplete = onComplete;
		}

		/// <inheritdoc />
		public bool Complete()
		{
			if(IsCompleted)
			{
				return true;
			}

			_activities[Id] = true;

			ProcessComplete();

			return IsCompleted;
		}

		/// <inheritdoc />
		public IWaitActivity Split()
		{
			var activity = new WaitActivity(InnerOnComplete);

			_activities.Add(activity.Id, false);

			return activity;
		}

		/// <inheritdoc />
		public void Reset()
		{
			IsCompleted = false;
		}

		private void InnerOnComplete(uint innerActivity)
		{
			_activities[innerActivity] = true;

			ProcessComplete();
		}

		private void ProcessComplete()
		{
			foreach (var activity in _activities)
			{
				if (!activity.Value)
				{
					return;
				}
			}

			IsCompleted = true;

			if (_innerOnComplete != null)
			{
				_innerOnComplete(Id);
			}
			else
			{
				_onComplete(null);
			}
		}
	}
}
