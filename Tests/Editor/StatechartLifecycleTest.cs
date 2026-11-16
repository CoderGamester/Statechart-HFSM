using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameLovers.StatechartMachine;
using GameLovers.StatechartMachine.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartLifecycleTest
	{
		private readonly IStatechartEvent _first = new StatechartEvent("First");
		private readonly IStatechartEvent _second = new StatechartEvent("Second");

		private List<string> _log;
		private TaskCompletionSource<bool> _taskSource;

		[SetUp]
		public void Init()
		{
			_log = new List<string>();
			_taskSource = new TaskCompletionSource<bool>();
		}

		[Test]
		// ADMIT: Statechart.MoveNext must queue an event triggered from a callback until the current step ends;
		// processing it inline runs it against the previous state, which drops it.
		// RCR: Statechart.cs MoveNext — `if (_isProcessing)` → `if (false)` → RED (log stops at "B enter end"; C never entered). 2026-11-15
		public void Trigger_FromEnterCallback_RunsAfterCurrentStep()
		{
			IStatechart statechart = null;

			statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var a = factory.State("A");
				var b = factory.State("B");
				var c = factory.State("C");

				initial.Transition().Target(a);
				a.Event(_first).Target(b);
				b.OnEnter(() =>
				{
					statechart.Trigger(_second);
					_log.Add("B enter end");
				});
				b.OnExit(() => _log.Add("B exit"));
				b.Event(_second).Target(c);
				c.OnEnter(() => _log.Add("C enter"));
			});

			statechart.Run();
			statechart.Trigger(_first);

			Assert.AreEqual(new[] { "B enter end", "B exit", "C enter" }, _log);
		}

		[Test]
		// ADMIT: Statechart.Process must stop a chain of automatic transitions when a callback pauses the chart,
		// and resume it on the next Run.
		// RCR: Statechart.cs Process — disable the `if (!_isRunning)` return in the chain loop → RED ("End enter" before the second Run). 2026-11-15
		public void Pause_FromEnterCallback_StopsChainUntilRun()
		{
			IStatechart statechart = null;

			statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var pausing = factory.Transition("Pausing");
				var end = factory.State("End");

				initial.Transition().Target(pausing);
				pausing.OnEnter(() => statechart.Pause());
				pausing.Transition().Target(end);
				end.OnEnter(() => _log.Add("End enter"));
			});

			statechart.Run();

			Assert.IsEmpty(_log);

			statechart.Run();

			Assert.AreEqual(new[] { "End enter" }, _log);
		}

		[Test]
		// ADMIT: TaskWaitState.InnerTaskAwait must complete the state when the awaited task throws; otherwise the
		// chart stays in the task state forever.
		// RCR: TaskWaitState.cs InnerTaskAwait — `return;` after ReportFault → RED (Final never entered). 2026-11-15
		public void TaskWait_TaskThrows_LogsAndCompletes()
		{
			var statechart = new Statechart(SetupTaskWait);

			statechart.Run();
			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Exception in the state 'Task'"));
			LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("Boom"));
			_taskSource.SetException(new InvalidOperationException("Boom"));

			Assert.AreEqual(new[] { "Final enter" }, _log);
		}

		[Test]
		// ADMIT: TaskWaitState.InnerTaskAwait must ignore a task that finishes after Reset; otherwise the stale
		// completion moves the reset chart forward.
		// RCR: TaskWaitState.cs InnerTaskAwait — drop `|| generation != Chart.Generation` → RED (stale completion enters Final). 2026-11-15
		public void Reset_TaskCompletesLater_DoesNotMoveChart()
		{
			var statechart = new Statechart(SetupTaskWait);
			var active = new List<StateNode>();

			statechart.Run();
			statechart.Reset();
			_taskSource.SetResult(true);
			statechart.GetActiveStates(active);

			Assert.IsEmpty(_log);
			Assert.AreEqual(1, active.Count);
			Assert.AreEqual(StateKind.Initial, active[0].Kind);
		}

		[Test]
		// ADMIT: WaitState.OnActivityComplete must ignore an activity completed after Reset, for the same reason.
		// RCR: WaitState.cs OnActivityComplete — drop `&& _generation == Chart.Generation` → RED (active state is Wait, not Initial). 2026-11-15
		public void Reset_WaitActivityCompletesLater_DoesNotMoveChart()
		{
			IWaitActivity activity = null;
			var active = new List<StateNode>();
			var statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.Wait("Wait");
				var final = factory.Final("Final");

				initial.Transition().Target(waiting);
				waiting.WaitingFor(waitActivity => activity = waitActivity).Target(final);
				final.OnEnter(() => _log.Add("Final enter"));
			});

			statechart.Run();
			statechart.Reset();
			activity.Complete();
			statechart.GetActiveStates(active);

			Assert.IsEmpty(_log);
			Assert.AreEqual(StateKind.Initial, active[0].Kind);
		}

		[Test]
		// ADMIT: Statechart.Dispose must cancel the token handed to token-aware task waits so their work stops.
		// RCR: Statechart.cs AbandonPendingWork — remove `_cancellation.Cancel();` → RED (token not cancelled). 2026-11-15
		public void Dispose_TokenAwareTaskWait_CancelsToken()
		{
			var token = CancellationToken.None;
			var statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var task = factory.TaskWait("Task");
				var final = factory.Final("Final");

				initial.Transition().Target(task);
				task.WaitingFor(cancellationToken =>
				{
					token = cancellationToken;
					return _taskSource.Task;
				}).Target(final);
			});

			statechart.Run();
			statechart.Dispose();

			Assert.IsTrue(token.IsCancellationRequested);
		}

		[Test]
		// ADMIT: Statechart.Run must refuse a disposed chart instead of silently running it again.
		// RCR: Statechart.cs Run — `if (_isDisposed)` → `if (false)` → RED (no exception). A task-wait chart hid this behind the disposed token source, hence the plain chart. 2026-11-15
		public void Run_AfterDispose_ThrowsObjectDisposedException()
		{
			var statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var state = factory.State("State");

				initial.Transition().Target(state);
				state.OnEnter(() => _log.Add("State enter"));
			});

			statechart.Dispose();

			Assert.Throws<ObjectDisposedException>(statechart.Run);
			Assert.IsEmpty(_log);
		}

		[Test]
		// ADMIT: SplitState.OnTrigger must run an action-only split event without force-completing the inner
		// waits; the split stays active, so its regions must keep waiting.
		// RCR: SplitState.cs OnTrigger — `if (transition.TargetState == null)` → `if (false)` → RED (inner wait advances to Final on the next event). Without the follow-up event it stays green. 2026-11-15
		public void Split_ActionOnlyEvent_KeepsInnerWaitRunning()
		{
			var active = new List<StateNode>();
			var statechart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var split = factory.Split("Split");

				initial.Transition().Target(split);
				split.Event(_first).OnTransition(() => _log.Add("action"));
				split.Split(SetupWaitRegion, SetupWaitRegion);
			});

			statechart.Run();
			statechart.Trigger(_first);
			// Any later event lets a force-completed wait advance, which is where the defect becomes visible.
			statechart.Trigger(_second);
			statechart.GetActiveStates(active);

			Assert.AreEqual(new[] { "action" }, _log);
			Assert.AreEqual(new[] { "Split", "Split/Wait", "Split/Wait" }, active.ConvertAll(state => state.Path));
		}

		[Test]
		// ADMIT: the Statechart constructor must reject a null setup with ArgumentNullException, not a
		// NullReferenceException from inside the package.
		// RCR: Statechart.cs Statechart(string, Action) — `if (setup == null)` → `if (false)` → RED (NullReferenceException). 2026-11-15
		public void Constructor_NullSetup_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new Statechart(null));
		}

		[Test]
		// ADMIT: StatechartUtils.RemoveGarbageFromStackTrace must drop every leading package frame so creation
		// traces start at the user code that declared the state.
		// RCR: StatechartUtils.cs IsPackageFrame — match the pre-0.8 "StateMachine.Internal." → RED (trace starts at StateInternal..ctor). 2026-11-15
		public void RemoveGarbageFromStackTrace_PackageFrames_StartsAtUserCode()
		{
			var trace = "  at System.Environment.get_StackTrace()\n" +
			            "  at GameLovers.StatechartMachine.Internal.StateInternal..ctor(String name)\n" +
			            "  at GameLovers.StatechartMachine.Internal.StateFactory.State(String name)\n" +
			            "  at Game.Player.Setup(IStateFactory factory)\n" +
			            "  at GameLovers.StatechartMachine.Statechart..ctor(String name, Action`1 setup)\n";

			var result = StatechartUtils.RemoveGarbageFromStackTrace(trace);

			StringAssert.StartsWith("  at Game.Player.Setup", result);
		}

		private static void SetupWaitRegion(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var waiting = factory.Wait("Wait");
			var final = factory.Final("Final");

			initial.Transition().Target(waiting);
			waiting.WaitingFor(_ => { }).Target(final);
		}

		private void SetupTaskWait(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var task = factory.TaskWait("Task");
			var final = factory.Final("Final");

			initial.Transition().Target(task);
			task.WaitingFor(() => _taskSource.Task).Target(final);
			final.OnEnter(() => _log.Add("Final enter"));
		}
	}
}
