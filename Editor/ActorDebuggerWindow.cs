using System.Text;
using DTech.ActorFramework.Diagnostics;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ActorDebuggerWindow : EditorWindow, IDebuggerCommands
	{
		private const string Title = "Actor Debugger";
		private const string USSName = "ActorDebuggerWindow";
		private const string RootClass = "actor-debugger";
		private const string ContentClass = "actor-debugger__content";
		private const string StatusClass = "actor-debugger__status";
		private const string ConnectionClass = "actor-debugger__connection";
		private const int TickMs = 100;
		private const int ActorsTab = 0;
		private const float ConnectionWidth = 160f;

		private static readonly string[] _tabNames = { "Actors", "Systems", "Queries" };

		private readonly DebugSession _session = new();
		private readonly LocalDebugSource _localSource = new();
		private readonly StringBuilder _statusBuilder = new();

		public bool IsRemote => _activeSource?.IsRemote ?? false;

		[SerializeField] private int _tabIndex;
		[SerializeField] private string _preferredWorldName;

		private RemoteDebugSource _remoteSource;
		private IConnectionState _connectionState;
		private IDebugSource _activeSource;
		private double _lastPollTime;
		private string _worldMenuSignature;
		private ToolbarMenu _worldMenu;
		private ToolbarToggle[] _tabToggles;
		private VisualElement[] _tabViews;
		private HelpBox _emptyBox;
		private VisualElement _content;
		private Label _status;
		private ActorsView _actorsView;
		private SystemsView _systemsView;
		private QueriesView _queriesView;

		[MenuItem("Window/DTech/" + Title)]
		public static void Open()
		{
			var window = GetWindow<ActorDebuggerWindow>();
			window.titleContent = new GUIContent(Title);
			window.minSize = new Vector2(560f, 320f);
			window.Show();
		}

		public void CreateGUI()
		{
			VisualElement root = rootVisualElement;
			root.Clear();
			root.AddToClassList(RootClass);
			StyleSheet styles = Resources.Load<StyleSheet>(USSName);
			if (styles != null)
			{
				root.styleSheets.Add(styles);
			}

			root.Add(CreateToolbar());

			_emptyBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
			root.Add(_emptyBox);

			_actorsView = new ActorsView(this);
			_systemsView = new SystemsView();
			_queriesView = new QueriesView(this);
			_tabViews = new[] { _actorsView.Element, _systemsView.Element, _queriesView.Element };

			_content = new VisualElement();
			_content.AddToClassList(ContentClass);
			for (int i = 0; i < _tabViews.Length; i++)
			{
				_content.Add(_tabViews[i]);
			}

			root.Add(_content);

			_status = new Label();
			_status.AddToClassList(StatusClass);
			root.Add(_status);

			SelectTab(_tabIndex);
			RefreshViews();
			root.schedule.Execute(Tick).Every(TickMs);
		}

		public void SetField(SetFieldRequest request)
		{
			_activeSource?.SetField(request);
			RequestPoll();
		}

		public void SelectActor(int actorId)
		{
			_session.SelectActor(actorId);
			SelectTab(ActorsTab);
			RequestPoll();
			RefreshViews();
		}

		public void ShowQueryActors(int queryIndex)
		{
			_session.SetQueryFilter(queryIndex);
			SelectTab(ActorsTab);
			RequestPoll();
			RefreshViews();
		}

		private void OnEnable()
		{
			_session.PreferredWorldName = _preferredWorldName;
			_connectionState = PlayerConnectionGUIUtility.GetConnectionState(this);
			_remoteSource = new RemoteDebugSource();
			_remoteSource.OnResponse += RemoteResponseHandler;
			_localSource.OnResponse += LocalResponseHandler;
		}

		private void OnDisable()
		{
			_localSource.OnResponse -= LocalResponseHandler;
			_remoteSource?.Dispose();
			_remoteSource = null;
			_connectionState?.Dispose();
			_connectionState = null;
			_activeSource = null;
		}

		private VisualElement CreateToolbar()
		{
			var toolbar = new Toolbar();

			var connection = new IMGUIContainer(DrawConnectionDropdown);
			connection.AddToClassList(ConnectionClass);
			connection.style.width = ConnectionWidth;
			toolbar.Add(connection);

			_worldMenu = new ToolbarMenu { text = "No worlds" };
			toolbar.Add(_worldMenu);

			_tabToggles = new ToolbarToggle[_tabNames.Length];
			for (int i = 0; i < _tabNames.Length; i++)
			{
				int tabIndex = i;
				var toggle = new ToolbarToggle { text = _tabNames[i] };
				toggle.RegisterValueChangedCallback(evt =>
				{
					if (evt.newValue)
					{
						SelectTab(tabIndex);
					}
					else if (_tabIndex == tabIndex)
					{
						toggle.SetValueWithoutNotify(true);
					}
				});
				_tabToggles[i] = toggle;
				toolbar.Add(toggle);
			}

			return toolbar;
		}

		private void DrawConnectionDropdown()
		{
			if (_connectionState == null)
			{
				return;
			}

			PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(_connectionState, EditorStyles.toolbarDropDown);
		}

		private void SelectTab(int tabIndex)
		{
			_tabIndex = Mathf.Clamp(tabIndex, 0, _tabNames.Length - 1);
			if (_tabViews == null)
			{
				return;
			}

			for (int i = 0; i < _tabViews.Length; i++)
			{
				bool isSelected = i == _tabIndex;
				_tabViews[i].style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
				_tabToggles[i].SetValueWithoutNotify(isSelected);
			}
		}

		private void Tick()
		{
			IDebugSource source = ResolveSource();
			if (source != _activeSource)
			{
				_activeSource = source;
				_session.Reset();
				RequestPoll();
				RefreshViews();
			}

			if (source.IsBusy)
			{
				return;
			}

			double now = EditorApplication.timeSinceStartup;
			if ((now - _lastPollTime) * 1000d < source.PollIntervalMs)
			{
				return;
			}

			_lastPollTime = now;
			source.Poll(_session.CreateRequest());
		}

		private IDebugSource ResolveSource()
		{
			bool isPlayer = _remoteSource != null && _connectionState != null &&
				_connectionState.connectedToTarget == ConnectionTarget.Player;

			return isPlayer ? _remoteSource : _localSource;
		}

		private void RequestPoll() => _lastPollTime = 0d;

		private void Apply(PollResponse response)
		{
			if (!_session.Apply(response))
			{
				RequestPoll();
			}

			_preferredWorldName = _session.PreferredWorldName;
			RefreshViews();
		}

		private void RefreshViews()
		{
			if (_content == null)
			{
				return;
			}

			UpdateWorldMenu();
			UpdateEmptyState();
			UpdateStatus();
			_actorsView.Show(_session);
			_systemsView.Show(_session);
			_queriesView.Show(_session);
		}

		private void UpdateWorldMenu()
		{
			WorldInfo[] worlds = _session.Worlds;
			_worldMenu.text = _session.SelectedWorld?.Name ?? "No worlds";

			_statusBuilder.Clear();
			for (int i = 0; i < worlds.Length; i++)
			{
				_statusBuilder.Append(worlds[i].Id).Append(':').Append(worlds[i].Name).Append('|');
			}

			_statusBuilder.Append(_session.SelectedWorldId);
			string signature = _statusBuilder.ToString();
			if (signature == _worldMenuSignature)
			{
				return;
			}

			_worldMenuSignature = signature;
			_worldMenu.menu.MenuItems().Clear();
			for (int i = 0; i < worlds.Length; i++)
			{
				WorldInfo world = worlds[i];
				_worldMenu.menu.AppendAction(world.Name, _ => SelectWorld(world.Id),
					_ => world.Id == _session.SelectedWorldId
						? DropdownMenuAction.Status.Checked
						: DropdownMenuAction.Status.Normal);
			}
		}

		private void SelectWorld(int worldId)
		{
			_session.SelectWorld(worldId);
			_preferredWorldName = _session.PreferredWorldName;
			RequestPoll();
			RefreshViews();
		}

		private void UpdateEmptyState()
		{
			string message = null;
			if (!_session.HasResponse)
			{
				message = IsRemote ? "Waiting for the player to answer…" : "Collecting worlds…";
			}
			else if (_session.Worlds.Length == 0)
			{
				message = IsRemote
					? "The player has no live ActorWorld. Is it a Development Build with the debugger enabled?"
					: "No live ActorWorld. Enter Play Mode, or pick a Development Build player in the connection dropdown.";
			}

			_emptyBox.text = message ?? string.Empty;
			_emptyBox.style.display = message == null ? DisplayStyle.None : DisplayStyle.Flex;
			_content.style.display = message == null ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void UpdateStatus()
		{
			WorldInfo world = _session.SelectedWorld;
			_statusBuilder.Clear();
			_statusBuilder.Append(IsRemote ? "Player" : "Editor");
			if (world != null)
			{
				_statusBuilder
					.Append(" · ").Append(world.Phase)
					.Append(" · Actors ").Append(world.ActorCount)
					.Append(" · Systems ").Append(world.SystemCount)
					.Append(" · Queries ").Append(world.QueryCount)
					.Append(" · Feature types ").Append(world.FeatureTypesUsed).Append('/')
					.Append(world.FeatureTypesCapacity);
			}

			_status.text = _statusBuilder.ToString();
		}

		private void LocalResponseHandler(PollResponse response)
		{
			if (_activeSource == _localSource)
			{
				Apply(response);
			}
		}

		private void RemoteResponseHandler(PollResponse response)
		{
			if (_activeSource == _remoteSource)
			{
				Apply(response);
			}
		}
	}
}