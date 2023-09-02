using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <inheritdoc />
	internal interface IWaitActivityInternal : IWaitActivity
	{
		/// <summary>
		/// Returns true if this activity has been marked as completed, false otherwise
		/// </summary>
		bool IsCompleted { get; }

		/// <summary>
		/// Forcely completes this activity and all its children that were splitted before
		/// </summary>
		void ForceComplete();
	}

	/// <inheritdoc />
	internal class WaitActivity : IWaitActivityInternal
	{
		private static uint _idRef;

		private readonly Action<uint> _onComplete;
		private readonly List<IWaitActivityInternal> _innerActivities = new List<IWaitActivityInternal>();

		private bool _completed;

		/// <inheritdoc />
		public uint Id { get; private set; }
		/// <inheritdoc />
		public bool IsCompleted => _completed && AreInnerCompleted();

		private WaitActivity()
		{
			Id = ++_idRef;
		}

		/// <summary>
		/// This constructor is called externally in <see cref="WaitState"/>
		/// </summary>
		public WaitActivity(Action<uint> onComplete) : this()
		{
			_onComplete = onComplete;
		}

		/// <inheritdoc />
		public bool Complete()
		{
			var innerCompleted = AreInnerCompleted();

			if (_completed && innerCompleted)
			{
				return true;
			}

			_completed = true;

			if (innerCompleted)
			{
				InvokeCompleted();
			}

			return _completed && innerCompleted;
		}

		/// <inheritdoc />
		public void ForceComplete()
		{
			_completed = true;

			foreach (var activity in _innerActivities)
			{
				activity.ForceComplete();
			}

			_innerActivities.Clear();
		}

		/// <inheritdoc />
		public IWaitActivity Split()
		{
			var activity = new WaitActivity(ProcessComplete);

			_innerActivities.Add(activity);

			return activity;
		}

		private void ProcessComplete(uint id)
		{
			if(!IsCompleted)
			{
				return;
			}

			InvokeCompleted();
		}

		private void InvokeCompleted()
		{
			_innerActivities.Clear();
			_onComplete(Id);
		}

		private bool AreInnerCompleted()
		{
			foreach (var activity in _innerActivities)
			{
				if (!activity.IsCompleted)
				{
					return false;
				}
			}

			return true;
		}
	}
}
