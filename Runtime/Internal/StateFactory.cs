using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	/// <inheritdoc />
	internal interface IStateFactoryInternal : IStateFactory
	{
		uint RegionLayer { get; }
		InitialState InitialState { get; }
		FinalState FinalState { get; }
		IList<IStateInternal> States { get; }
		StateFactoryData Data { get; }
		void Add(IList<IStateInternal> states);
	}

	/// <inheritdoc />
	internal class StateFactory : IStateFactoryInternal
	{
		public uint RegionLayer { get; private set; }
		public InitialState InitialState { get; private set; }
		public FinalState FinalState { get; private set; }
		public IList<IStateInternal> States { get; private set; }
		public StateFactoryData Data { get; private set; }

		private StateFactory()
		{
		}

		public StateFactory(uint layer, StateFactoryData data)
		{
			States = new List<IStateInternal>();

			Data = data;
			RegionLayer = layer;
		}

		/// <inheritdoc />
		public void Add(IList<IStateInternal> states)
		{
			foreach (var state in states)
			{
				States.Add(state);
			}
		}

		/// <inheritdoc />
		public IInitialState Initial(string name)
		{
			if (InitialState != null)
			{
				throw new InvalidOperationException($"Initial state already set to {InitialState.Name}");
			}

			var initial = new InitialState(name, this);

			InitialState = initial;

			States.Add(initial);

			return initial;
		}

		/// <inheritdoc />
		public IFinalState Final(string name)
		{
			if (FinalState != null)
			{
				throw new InvalidOperationException($"Final state already set to {FinalState.Name}");
			}

			var state = new FinalState(name, this);

			FinalState = state;

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ISimpleState State(string name)
		{
			var state = new SimpleState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public INestState Nest(string name)
		{
			var state = new NestState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public IWaitState Wait(string name)
		{
			var state = new WaitState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ITaskWaitState TaskWait(string name)
		{
			var state = new TaskWaitState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public IChoiceState Choice(string name)
		{
			var state = new ChoiceState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ISplitState Split(string name)
		{
			var state = new SplitState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ILeaveState Leave(string name)
		{
			var state = new LeaveState(name, this);

			States.Add(state);

			return state;
		}
	}
}