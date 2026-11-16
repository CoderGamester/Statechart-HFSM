using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Samples.GameFlow
{
	/// <summary>
	/// A small game loop driven by one statechart that uses every state kind: a nested boot sequence with task waits,
	/// a menu, a split gameplay state with a player region and a waves region, choices, leave states and results.
	/// </summary>
	public class GameFlowSample : MonoBehaviour
	{
		private const int WaveCount = 3;
		private const int ActivityLines = 14;

		[SerializeField] private UIDocument _document;

		private readonly IStatechartEvent _play = new StatechartEvent("Play");
		private readonly IStatechartEvent _jump = new StatechartEvent("Jump");
		private readonly IStatechartEvent _hit = new StatechartEvent("Hit");
		private readonly IStatechartEvent _quit = new StatechartEvent("Quit");
		private readonly IStatechartEvent _retry = new StatechartEvent("Retry");
		private readonly Queue<string> _activity = new Queue<string>();
		private readonly List<StateNode> _active = new List<StateNode>();

		private IStatechart _statechart;
		private int _wave;
		private bool _won;
		private Label _phase;
		private Label _detail;
		private Label _waves;
		private Label _activityLog;
		private VisualElement _player;
		private Button _playButton;
		private Button _jumpButton;
		private Button _hitButton;
		private Button _quitButton;
		private Button _retryButton;

		private void Awake()
		{
			var root = _document.rootVisualElement;

			_phase = root.Q<Label>("phase");
			_detail = root.Q<Label>("detail");
			_waves = root.Q<Label>("waves");
			_activityLog = root.Q<Label>("activity");
			_player = root.Q("player");
			_playButton = root.Q<Button>("play");
			_jumpButton = root.Q<Button>("jump");
			_hitButton = root.Q<Button>("hit");
			_quitButton = root.Q<Button>("quit");
			_retryButton = root.Q<Button>("retry");

			_playButton.clicked += () => _statechart.Trigger(_play);
			_jumpButton.clicked += () => _statechart.Trigger(_jump);
			_hitButton.clicked += () => _statechart.Trigger(_hit);
			_quitButton.clicked += () => _statechart.Trigger(_quit);
			_retryButton.clicked += () => _statechart.Trigger(_retry);

			_statechart = new Statechart("Game Flow", Setup);
			_statechart.Traced += OnTraced;
		}

		private void Start()
		{
			_statechart.Run();
		}

		private void Update()
		{
			_statechart.GetActiveStates(_active);
			_playButton.SetEnabled(IsActive("Menu"));
			_jumpButton.SetEnabled(IsActive("Gameplay/Idle"));
			_hitButton.SetEnabled(IsActive("Gameplay/Idle") || IsActive("Gameplay/Airborne"));
			_quitButton.SetEnabled(IsActive("Gameplay"));
			_retryButton.SetEnabled(IsActive("Results"));
			_waves.text = IsActive("Gameplay") ? $"Wave {Mathf.Min(_wave + 1, WaveCount)}/{WaveCount}" : "";
		}

		private void OnDestroy()
		{
			_statechart?.Dispose();
		}

		private void Setup(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var boot = factory.Nest("Boot");
			var menu = factory.State("Menu");
			var gameplay = factory.Split("Gameplay");
			var results = factory.State("Results");
			var resetScore = factory.Transition("Reset Score");

			initial.Transition().Target(boot);

			boot.OnEnter(() => Show("Booting", "Loading config"));
			boot.Nest(SetupBoot).Target(menu);

			menu.OnEnter(() => Show("Menu", "Press Play"));
			menu.Event(_play).Target(gameplay);

			gameplay.OnEnter(() =>
			{
				_wave = 0;
				Show("Gameplay", "Survive the waves");
			});
			gameplay.Split(SetupPlayer, region => SetupWaves(region, results)).Target(menu);
			gameplay.Event(_quit).Target(menu);
			gameplay.OnExit(() => _player.EnableInClassList("gf-player--airborne", false));

			results.OnEnter(() => Show("Results", _won ? "Victory! All waves cleared" : "Defeated"));
			results.Event(_retry).Target(resetScore);

			resetScore.OnEnter(() => _won = false);
			resetScore.Transition().Target(menu);

			void SetupPlayer(IStateFactory region)
			{
				var playerInitial = region.Initial("Player Initial");
				var idle = region.State("Idle");
				var airborne = region.Wait("Airborne");
				var defeated = region.Leave("Defeated");

				playerInitial.Transition().Target(idle);

				idle.OnEnter(() => _player.EnableInClassList("gf-player--down", false));
				idle.Event(_jump).Target(airborne);
				idle.Event(_hit).Target(defeated);

				airborne.OnEnter(() => _player.EnableInClassList("gf-player--airborne", true));
				airborne.WaitingFor(activity => StartCoroutine(CompleteAfter(activity, 0.6f))).Target(idle);
				airborne.Event(_hit).Target(defeated);
				airborne.OnExit(() => _player.EnableInClassList("gf-player--airborne", false));

				defeated.OnEnter(() =>
				{
					_won = false;
					_player.EnableInClassList("gf-player--down", true);
				});
				defeated.Transition().Target(results);
			}
		}

		private void SetupBoot(IStateFactory factory)
		{
			var initial = factory.Initial("Boot Initial");
			var loadConfig = factory.TaskWait("Load Config");
			var warmUp = factory.TaskWait("Warm Up");
			var final = factory.Final("Boot Final");

			initial.Transition().Target(loadConfig);
			loadConfig.WaitingFor(token => FakeWork(900, token)).Target(warmUp);
			warmUp.OnEnter(() => Show("Booting", "Warming up"));
			warmUp.WaitingFor(token => FakeWork(700, token)).Target(final);
		}

		private void SetupWaves(IStateFactory factory, IState results)
		{
			var initial = factory.Initial("Waves Initial");
			var spawning = factory.Wait("Spawning");
			var moreWaves = factory.Choice("More Waves?");
			var nextWave = factory.Transition("Next Wave");
			var victory = factory.Leave("Victory");

			initial.Transition().Target(spawning);
			spawning.WaitingFor(activity => StartCoroutine(CompleteAfter(activity, 2.5f))).Target(moreWaves);
			moreWaves.Transition().Condition(() => _wave + 1 < WaveCount).Target(nextWave);
			moreWaves.Transition().Target(victory);
			nextWave.OnEnter(() => _wave++);
			nextWave.Transition().Target(spawning);
			victory.OnEnter(() => _won = true);
			victory.Transition().Target(results);
		}

		private static async UniTask FakeWork(int milliseconds, CancellationToken token)
		{
			await UniTask.Delay(milliseconds, cancellationToken: token);
		}

		private static IEnumerator CompleteAfter(IWaitActivity activity, float seconds)
		{
			yield return new WaitForSeconds(seconds);

			activity.Complete();
		}

		private bool IsActive(string path)
		{
			foreach (var state in _active)
			{
				if (state.Path == path)
				{
					return true;
				}
			}

			return false;
		}

		private void Show(string phase, string detail)
		{
			_phase.text = phase;
			_detail.text = detail;
		}

		private void OnTraced(StatechartTrace trace)
		{
			string line;

			switch (trace.Kind)
			{
				case StatechartTraceKind.EventTriggered:
					line = $"Event: {trace.Event.Name}";
					break;
				case StatechartTraceKind.StateEntered:
					line = $"  enter {trace.State.Path}";
					break;
				default:
					return;
			}

			if (_activity.Count == ActivityLines)
			{
				_activity.Dequeue();
			}

			_activity.Enqueue(line);
			_activityLog.text = string.Join("\n", _activity);
		}
	}
}
