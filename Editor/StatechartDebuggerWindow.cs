using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Editor
{
	/// <summary>
	/// Live debugger for every running <see cref="Statechart"/>. Shows the selected chart as a live chart of nested
	/// boxes and as a state tree with its trace log; the toolbar toggles turn each view on or off.
	/// </summary>
	public class StatechartDebuggerWindow : EditorWindow
	{
		private const string StyleSheetPath = "Packages/com.gamelovers.statechart/Editor/StatechartDebugger.uss";
		private const long RefreshIntervalMs = 100;

		[SerializeField] private string _selectedChartName;
		[SerializeField] private bool _showLiveChart = true;
		[SerializeField] private bool _showTreeTrace = true;

		private readonly List<IStateNode> _activeStates = new List<IStateNode>();

		private StatechartRegistry.Entry _selected;
		private IStatechart _selectedChart;
		private int _renderedVersion = -1;
		private ToolbarMenu _chartMenu;
		private Label _statusLabel;
		private VisualElement _body;
		private Label _emptyLabel;
		private LiveChartView _liveChartView;
		private TreeTraceView _treeTraceView;

		[MenuItem("Tools/GameLovers/Statechart Debugger", priority = 100)]
		private static void Open()
		{
			var window = GetWindow<StatechartDebuggerWindow>();

			window.titleContent = new GUIContent("Statechart Debugger");
			window.minSize = new Vector2(480, 320);
			window.Show();
		}

		private void CreateGUI()
		{
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);

			if (styleSheet != null)
			{
				rootVisualElement.styleSheets.Add(styleSheet);
			}

			rootVisualElement.AddToClassList("sc-root");
			rootVisualElement.Add(BuildToolbar());

			_emptyLabel = new Label("No live statecharts. Enter Play mode, or create a Statechart from code.");
			_emptyLabel.AddToClassList("sc-empty");
			rootVisualElement.Add(_emptyLabel);

			_body = new VisualElement();
			_body.AddToClassList("sc-body");
			rootVisualElement.Add(_body);

			_liveChartView = new LiveChartView();
			_treeTraceView = new TreeTraceView();
			_body.Add(_liveChartView);
			_body.Add(_treeTraceView);

			StatechartRegistry.Changed += OnRegistryChanged;
			rootVisualElement.RegisterCallback<DetachFromPanelEvent>(_ => StatechartRegistry.Changed -= OnRegistryChanged);
			rootVisualElement.schedule.Execute(Refresh).Every(RefreshIntervalMs);

			ApplyViewVisibility();
			SelectChart(null);
		}

		private Toolbar BuildToolbar()
		{
			var toolbar = new Toolbar();

			_chartMenu = new ToolbarMenu { text = "Select chart" };
			_chartMenu.AddToClassList("sc-chart-menu");
			_chartMenu.RegisterCallback<PointerDownEvent>(_ => PopulateChartMenu(), TrickleDown.TrickleDown);
			toolbar.Add(_chartMenu);

			_statusLabel = new Label();
			_statusLabel.AddToClassList("sc-status");
			toolbar.Add(_statusLabel);

			toolbar.Add(new ToolbarButton(() => _selectedChart?.Run()) { text = "Run", tooltip = "Run or resume the chart" });
			toolbar.Add(new ToolbarButton(() => _selectedChart?.Pause()) { text = "Pause" });
			toolbar.Add(new ToolbarButton(() => _selectedChart?.Reset()) { text = "Reset", tooltip = "Reset to the initial state" });
			toolbar.Add(new ToolbarSpacer { flex = true });

			toolbar.Add(CreateViewToggle("Live chart", _showLiveChart, value => _showLiveChart = value));
			toolbar.Add(CreateViewToggle("Tree + trace", _showTreeTrace, value => _showTreeTrace = value));

			var copyMenu = new ToolbarMenu { text = "Copy" };
			copyMenu.menu.AppendAction("Mermaid diagram", _ => Copy(StatechartExport.ToMermaid),
				_ => _selectedChart != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
			copyMenu.menu.AppendAction("Active state paths", _ => Copy(StatechartExport.ToActivePaths),
				_ => _selectedChart != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
			copyMenu.menu.AppendAction("Trace history", _ => CopyTrace(),
				_ => _selected != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
			toolbar.Add(copyMenu);

			return toolbar;
		}

		private ToolbarToggle CreateViewToggle(string label, bool initial, Action<bool> setter)
		{
			var toggle = new ToolbarToggle { text = label, value = initial };

			toggle.RegisterValueChangedCallback(evt =>
			{
				setter(evt.newValue);
				ApplyViewVisibility();
			});

			return toggle;
		}

		private void ApplyViewVisibility()
		{
			_liveChartView.style.display = _showLiveChart ? DisplayStyle.Flex : DisplayStyle.None;
			_treeTraceView.style.display = _showTreeTrace ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void PopulateChartMenu()
		{
			var menu = _chartMenu.menu;

			menu.ClearItems();

			foreach (var entry in StatechartRegistry.GetEntries())
			{
				var captured = entry;

				menu.AppendAction(entry.Name, _ => SelectChart(captured),
					_ => captured == _selected ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
			}

			if (StatechartRegistry.GetEntries().Count == 0)
			{
				menu.AppendAction("No live statecharts", null, DropdownMenuAction.Status.Disabled);
			}
		}

		private void OnRegistryChanged()
		{
			if (_selected == null || !_selected.TryGetChart(out _) || !Contains(StatechartRegistry.GetEntries(), _selected))
			{
				SelectChart(null);
			}
		}

		private void SelectChart(StatechartRegistry.Entry entry)
		{
			var entries = StatechartRegistry.GetEntries();

			if (entry == null)
			{
				// Prefer the last selected name so the window survives domain reloads and play mode restarts.
				foreach (var candidate in entries)
				{
					if (candidate.Name == _selectedChartName)
					{
						entry = candidate;
						break;
					}
				}

				entry ??= entries.Count > 0 ? entries[entries.Count - 1] : null;
			}

			_selected = entry;
			_selectedChart = null;
			entry?.TryGetChart(out _selectedChart);
			_selectedChartName = entry?.Name ?? _selectedChartName;
			_renderedVersion = -1;

			_chartMenu.text = entry?.Name ?? "Select chart";
			_emptyLabel.style.display = _selectedChart == null ? DisplayStyle.Flex : DisplayStyle.None;
			_body.style.display = _selectedChart == null ? DisplayStyle.None : DisplayStyle.Flex;

			_liveChartView.SetChart(_selectedChart);
			_treeTraceView.SetChart(_selectedChart);
			Refresh();
		}

		private void Refresh()
		{
			if (_selected != null && !_selected.TryGetChart(out _))
			{
				SelectChart(null);
				return;
			}

			if (_selectedChart == null)
			{
				if (StatechartRegistry.GetEntries().Count > 0)
				{
					SelectChart(null);
				}

				return;
			}

			_statusLabel.text = _selectedChart.IsRunning ? "Running" : "Paused";
			_statusLabel.EnableInClassList("sc-status-running", _selectedChart.IsRunning);

			if (_renderedVersion == StatechartRegistry.Version)
			{
				return;
			}

			_renderedVersion = StatechartRegistry.Version;
			_selectedChart.GetActiveStates(_activeStates);
			_liveChartView.UpdateActive(_activeStates, _selected.History);
			_treeTraceView.UpdateActive(_activeStates, _selected.History);
		}

		private void Copy(Func<IStatechart, string> export)
		{
			if (_selectedChart != null)
			{
				EditorGUIUtility.systemCopyBuffer = export(_selectedChart);
			}
		}

		private void CopyTrace()
		{
			if (_selected == null)
			{
				return;
			}

			var lines = new List<string>();

			foreach (var record in _selected.History)
			{
				lines.Add($"#{record.Frame} {record.Message}");
			}

			EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
		}

		private static bool Contains(IReadOnlyList<StatechartRegistry.Entry> entries, StatechartRegistry.Entry entry)
		{
			for (var i = 0; i < entries.Count; i++)
			{
				if (entries[i] == entry)
				{
					return true;
				}
			}

			return false;
		}
	}
}
