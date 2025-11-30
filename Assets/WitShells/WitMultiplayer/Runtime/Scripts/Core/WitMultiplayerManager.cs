using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using WitShells.DesignPatterns;
using WitShells.DesignPatterns.Core;

namespace WitShells.WitMultiplayer
{
    public enum MatchType
    {
        Lobby,
        Matchmaker
    }

    public class WitMultiplayerManager : MonoSingleton<WitMultiplayerManager>
    {
        [SerializeField]
        private MatchType matchType;

        private bool _isInitialized = false;

        private CancellationTokenSource _matchmakingCts;
        private LobbyManager _lobbyManager;

        public string PlayerId
        {
            get
            {
                return AuthenticationService.Instance.PlayerId;
            }
        }

        public MatchType CurrentMatchType
        {
            get
            {
                return matchType;
            }
            set
            {
                matchType = value;
            }
        }

        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
        }

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        private void OnApplicationQuit()
        {
            WitLogger.Log("Application quitting, leaving lobby if joined.");
            _lobbyManager?.Leave(PlayerId);
            SignOut();
        }

        private IEnumerator Initialize()
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            {
                UnityServices.Initialized += OnInitialized;
                UnityServices.InitializeFailed += OnInitializeFailed;
                yield return UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.ClearSessionToken();

                AuthenticationService.Instance.SignedIn += OnSignedIn;
                AuthenticationService.Instance.SignInFailed += OnSignInFailed;
                AuthenticationService.Instance.SignedOut += OnSignedOut;
                yield return AuthenticationService.Instance.SignInAnonymouslyAsync();

                WitLogger.Log("Signed in anonymously with Player ID: " + AuthenticationService.Instance.PlayerId);
            }
        }

        public void CreateOrJoinLobby(string mode, ushort maxPlayers, string password = "")
        {
            StartCoroutine(CreateOrJoinLobbyCoroutine(mode, maxPlayers, password));
        }

        private IEnumerator CreateOrJoinLobbyCoroutine(string mode, ushort maxPlayers, string password = "")
        {
            if (_lobbyManager != null)
            {
                yield return _lobbyManager.LeaveLobby(PlayerId, () =>
                {
                    _lobbyManager = null;
                    CreateOrJoinLobby(mode, maxPlayers, password);
                }, (error) =>
                {
                    WitLogger.LogError($"Failed to leave existing lobby: {error.Message}");
                });
                yield break;
            }

            var player = NetworkingUtils.GetNewPlayer(PlayerId);

            var options = NetworkingUtils.LobbyOptions(player, password, !string.IsNullOrEmpty(password), null);

            yield return NetworkingUtils.QuickJoinOrCreate(
                player, mode, maxPlayers,
                options
                , (lobby) =>
                {
                    WitLogger.Log("Joined or created lobby with ID: " + lobby.Id);
                    _lobbyManager = new LobbyManager(lobby);
                    RegisterLobbyEvents();
                }, (error) =>
                {
                    WitLogger.LogError("Failed to join or create lobby: " + error.Message);
                }
            );
        }

        private void RegisterLobbyEvents()
        {
            if (_lobbyManager == null) return;
            _lobbyManager.LobbyChanged += OnLobbyChanged;
            _lobbyManager.PlayerJoined += OnPlayerJoined;
            _lobbyManager.PlayerLeft += OnPlayerLeft;
            _lobbyManager.LobbyDeleted += OnLobbyDeleted;
            _lobbyManager.PlayerDataAdded += OnPlayerDataAdded;
            _lobbyManager.KickedFromLobby += OnKickedFromLobby;
            _lobbyManager.PlayerDataChanged += OnPlayerDataChanged;
            _lobbyManager.PlayerDataRemoved += OnPlayerDataRemoved;
            _lobbyManager.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
        }

        #region Lobby Event Handlers

        private void OnLobbyChanged(ILobbyChanges changes)
        {
            WitLogger.Log("Lobby changed: " + changes.ToString());
            // Handle lobby changes, e.g., update UI or refresh lobby data
        }

        private void OnPlayerJoined(List<LobbyPlayerJoined> joined)
        {
            foreach (var player in joined)
            {
                WitLogger.Log($"Player joined");
                // Update player list UI
            }
        }

        private void OnPlayerLeft(List<int> leftPlayerIndexes)
        {
            foreach (var index in leftPlayerIndexes)
            {
                WitLogger.Log($"Player left at index: {index}");
                // Remove player from UI
            }
        }

        private void OnLobbyDeleted()
        {
            WitLogger.Log("Lobby has been deleted.");
            // Handle lobby deletion, e.g., return to menu
            _lobbyManager = null;
        }

        private void OnPlayerDataAdded(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
        {
            foreach (var kvp in changes)
            {
                int playerIndex = kvp.Key;
                foreach (var data in kvp.Value)
                {
                    WitLogger.Log($"Player data added for player {playerIndex}: {data.Key} = {data.Value.Value}");
                    // Update player data UI
                }
            }
        }

        private void OnKickedFromLobby()
        {
            WitLogger.Log("You have been kicked from the lobby.");
            // Handle kick, e.g., return to menu
            _lobbyManager = null;
        }

        private void OnPlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
        {
            foreach (var kvp in changes)
            {
                int playerIndex = kvp.Key;
                foreach (var data in kvp.Value)
                {
                    WitLogger.Log($"Player data changed for player {playerIndex}: {data.Key} = {data.Value.Value}");
                    // Update player data UI
                }
            }
        }

        private void OnPlayerDataRemoved(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
        {
            foreach (var kvp in changes)
            {
                int playerIndex = kvp.Key;
                foreach (var data in kvp.Value)
                {
                    WitLogger.Log($"Player data removed for player {playerIndex}: {data.Key}");
                    // Update player data UI
                }
            }
        }

        private void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
        {
            WitLogger.Log($"Lobby event connection state changed: {state}");
            // Handle connection state, e.g., show connection status
        }

        #endregion

        public void StartMatchMaking()
        {
            _matchmakingCts = new CancellationTokenSource();
            StartCoroutine(NetworkingUtils.StartMatchMaking(_matchmakingCts,
                onComplete: session =>
                {
                    WitLogger.Log("Matchmaking completed. Session ID: " + session.Id);
                    NetworkManager.Singleton.StartClient();
                },
                onError: exception =>
                {
                    WitLogger.LogError("Matchmaking failed: " + exception.Message);
                }));
        }

        public void StartMatchMaking(ushort maxPlayers, string queueName, string sessionType)
        {
            _matchmakingCts = new CancellationTokenSource();
            StartCoroutine(NetworkingUtils.StartMatchMaking(_matchmakingCts,
                onComplete: session =>
                {
                    WitLogger.Log("Matchmaking completed. Session ID: " + session.Id);
                    NetworkManager.Singleton.StartClient();
                },
                onError: exception =>
                {
                    WitLogger.LogError("Matchmaking failed: " + exception.Message);
                }, sessionType, maxPlayers, queueName));
        }

        public void StopMatchMaking()
        {
            if (_matchmakingCts != null)
            {
                _matchmakingCts.Cancel();
                _matchmakingCts.Dispose();
                _matchmakingCts = null;
            }
        }

        #region Initialization

        private void OnInitialized()
        {
            _isInitialized = true;
        }

        private void OnInitializeFailed(Exception exception)
        {
            _isInitialized = false;
            WitLogger.LogError($"Initialization failed: {exception.Message}");
        }

        #endregion


        #region Authentication

        public void SignOut()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
        }

        public void OnSignedIn()
        {

        }

        public void OnSignInFailed(Exception exception)
        {

        }

        public void OnSignedOut()
        {

        }

        #endregion


        #region Utility Methods


        public bool IsSignedIn
        {
            get
            {
                return AuthenticationService.Instance.IsSignedIn;
            }
        }

        #endregion

        #region Editor Methods
#if UNITY_EDITOR

        [UnityEditor.MenuItem("WitShells/Wit Multiplayer/Initialize Wit Multiplayer Manager")]
        private static void InitializeWitMultiplayerManager()
        {
            var existingManager = FindAnyObjectByType<WitMultiplayerManager>();
            if (existingManager != null)
            {
                UnityEditor.Selection.activeGameObject = existingManager.gameObject;
                WitLogger.Log("Wit Multiplayer Manager already exists in the scene. Selected the existing instance.");
                return;
            }

            var managerGameObject = new GameObject("WitMultiplayerManager");
            var manager = managerGameObject.AddComponent<WitMultiplayerManager>();
            UnityEditor.Selection.activeGameObject = managerGameObject;
            WitLogger.Log("Wit Multiplayer Manager has been added to the scene.");
        }

        [ContextMenu("Test Matchmaking")]
        private void TestMatchmaking()
        {
            StartMatchMaking();
        }

        [ContextMenu("Test Stop Matchmaking")]
        private void TestStopMatchmaking()
        {
            StopMatchMaking();
        }

        [ContextMenu("Test Create or Join Lobby")]
        private void TestCreateOrJoinLobby()
        {
            CreateOrJoinLobby("defaultLobby", 2, "");
        }

#endif
        #endregion
    }
}