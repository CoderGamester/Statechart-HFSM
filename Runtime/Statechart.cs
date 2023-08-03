using System;
using GameLovers.StatechartMachine.Internal;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine
{
	/// <summary>
	 /// Interface to help debug the state chart
	 /// </summary>
	public interface IStateMachineDebug
	{
		/// <summary>
		/// Allows to show logs in the console to help debugging possible errors in the states & transitions
		/// </summary>
		bool LogsEnabled { get; set; }
	}

	/// <summary>
	/// The main object which represents the State Chart and drives it forward.
	/// The State Chart schematics are defined in the constructor setup action and cannot be modified during runtime. 
	/// See <see cref="http://www.omg.org/spec/UML"/> for Semantics.
	/// </summary>
	public interface IStatechart : IStateMachineDebug
	{
		/// <summary>
		/// Processes the event <param name="trigger"></param> for the State Chart with run-to-completion paradigm.
		/// Triggers only work if the State Chart is not paused.
		/// </summary>
		void Trigger(IStatechartEvent trigger);
		
		/// <summary>
		/// Start/Resume the control of the State Chart from where is anchored.
		/// Does nothing if already running.
		/// </summary>
		void Run();
		
		/// <summary>
		/// Pauses the control of the State Chart.
		/// Call <see cref="Run"/> to resume it.
		/// </summary>
		void Pause();
		
		/// <summary>
		/// Resets the State Chart to it's initial starting point.
		/// This call doesn't pause or resume the control of the State Chart. If the State Chart is in waiting
		/// mode for an event or paused, it will require a call <see cref="Run"/> to resume it.
		/// </summary>
		void Reset();
	}

	/// <inheritdoc cref="IStatechart"/>
	public class Statechart : IStatechart
	{
		private bool _isRunning;
		private IStateInternal _currentState;

		private readonly IStateFactoryInternal _stateFactory;

		/// <inheritdoc />
		public bool LogsEnabled { get; set; }

#if DEVELOPMENT_BUILD
		public Stopwatch StateWatch = new ();
		public static Action<string, long> OnStateTimed;
#endif

#if UNITY_EDITOR
		public string CurrentState => _currentState.Name;
#endif
		
		private Statechart() {}

		public Statechart(Action<IStateFactory> setup)
		{
			var stateFactory = new StateFactory(0, new StateFactoryData { Statechart = this, StateChartMoveNextCall = MoveNext });

			setup(stateFactory);

			_stateFactory = stateFactory;

			if (_stateFactory.InitialState == null)
			{
				throw new MissingMemberException("State chart doesn't have initial state");
			}

			_currentState = _stateFactory.InitialState;

#if UNITY_EDITOR || DEBUG
			for(int i = 0; i < _stateFactory.States.Count; i++)
			{
				_stateFactory.States[i].Validate();
			}
#endif
		}

		/// <inheritdoc />
		public void Trigger(IStatechartEvent trigger)
		{
			if (!_isRunning)
			{
				return;
			}

			MoveNext(trigger);
		}

		/// <inheritdoc />
		public void Run()
		{
			_isRunning = true;

			MoveNext(null);
		}

		/// <inheritdoc />
		public void Pause()
		{
			_isRunning = false;
		}

		/// <inheritdoc />
		public void Reset()
		{
			_currentState = _stateFactory.InitialState;
		}

#if DEVELOPMENT_BUILD
		private void MeasureTime()
		{
			if (StateWatch.IsRunning)
			{
				StateWatch.Stop();
				OnStateTimed?.Invoke(_currentState.Name, StateWatch.ElapsedMilliseconds);
				StateWatch.Reset();
			}
		}
#endif

		private void MoveNext(IStatechartEvent trigger)
		{
#if DEVELOPMENT_BUILD
			if (StateWatch.IsRunning && _currentState != null)
			{
				MeasureTime();
			}
#endif
			var nextState = _currentState.Trigger(trigger);
			while (nextState != null)
			{
#if DEVELOPMENT_BUILD
				StateWatch.Start();
#endif
				_currentState = nextState;
				nextState = _currentState.Trigger(null);
				
				
			}
		}
	}
}