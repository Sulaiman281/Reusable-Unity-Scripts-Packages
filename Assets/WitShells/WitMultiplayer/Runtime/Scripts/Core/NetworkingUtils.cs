using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns;

namespace WitShells.WitMultiplayer
{
    public static class NetworkingUtils
    {
        public static Player GetNewPlayer(string playerId)
        {
            return new Player(playerId);
        }

        #region Lobby Methods

        public static CreateLobbyOptions LobbyOptions(Player player, string password, bool isPrivate, Dictionary<string, DataObject> data)
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.Player = player;
            lobbyOptions.Password = string.IsNullOrEmpty(password) ? null : password;
            lobbyOptions.IsPrivate = isPrivate;
            lobbyOptions.Data = data == null ? new Dictionary<string, DataObject>() : data;
            return lobbyOptions;
        }

        public static IEnumerator CreateNewLobby(string name, int maxPlayers, CreateLobbyOptions lobbyOptions,
             UnityAction<Lobby> onComplete, UnityAction<Exception> onError)
        {
            var task = LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, lobbyOptions);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                onError?.Invoke(task.Exception);
            }
            else
            {
                onComplete?.Invoke(task.Result);
            }
        }

        public static IEnumerator UpdateLobby(Lobby currentLobby, string key, DataObject dataObject, UnityAction<Lobby> onComplete, UnityAction<Exception> onError)
        {
            if (currentLobby == null) throw new InvalidOperationException("No lobby to update.");

            var data = new Dictionary<string, DataObject>
            {
                [key] = dataObject
            };

            var task = LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions { Data = data });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                onError?.Invoke(task.Exception);
            }
            else
            {
                onComplete?.Invoke(task.Result);
            }
        }

        public static IEnumerator LeaveLobby(this LobbyManager lobbyManager, string playerId, UnityAction onComplete, UnityAction<Exception> onError)
        {
            if (lobbyManager.Lobby == null)
                yield break;

            if (lobbyManager.IsHost(playerId))
            {
                lobbyManager.StopHostHeartBeat();

                var task = LobbyService.Instance.DeleteLobbyAsync(lobbyManager.Lobby.Id);
                yield return new WaitUntil(() => task.IsCompleted);
            }
            else
            {
                lobbyManager.Leave(playerId);
            }
        }

        public static IEnumerator QueryLobbies(int count, bool includePrivate, UnityAction<List<Lobby>> onComplete, UnityAction<Exception> onError, Dictionary<string, string> filters = null)
        {
            // Guard defaults
            if (count <= 0) count = 20;

            var options = new QueryLobbiesOptions { Count = count };

            var task = LobbyService.Instance.QueryLobbiesAsync(options);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                onError?.Invoke(task.Exception);
                yield break;
            }

            try
            {
                var results = task.Result?.Results ?? new List<Lobby>();

                var filtered = new List<Lobby>();

                foreach (var r in results)
                {
                    var lobby = r;
                    if (lobby == null) continue;

                    WitLogger.Log($"Found Lobby: {lobby.Name} ({lobby.Id})");

                    // only public lobbies
                    if (!includePrivate && lobby.IsPrivate)
                    {
                        WitLogger.Log($"Skipping private lobby: {lobby.Name} ({lobby.Id})");
                        continue;
                    }

                    // only available (has free slots)
                    if (lobby.Players != null && lobby.Players.Count >= lobby.MaxPlayers)
                    {
                        WitLogger.Log($"Skipping full lobby: {lobby.Name} ({lobby.Id})");
                        continue;
                    }

                    // mode filter (if supplied) - lobby.Data is Dictionary<string, DataObject>
                    if (filters != null)
                    {
                        bool modeMismatch = false;
                        foreach (var filter in filters)
                        {
                            if (lobby.Data != null && lobby.Data.TryGetValue(filter.Key, out var dataObject))
                            {
                                if (dataObject.Value != filter.Value)
                                {
                                    WitLogger.Log($"Skipping lobby due to filter mismatch: {lobby.Name} ({lobby.Id}) - Key: {filter.Key}, Expected: {filter.Value}, Actual: {dataObject.Value}");
                                    modeMismatch = true;
                                    break;
                                }
                            }
                            else
                            {
                                WitLogger.Log($"Skipping lobby due to missing filter key: {lobby.Name} ({lobby.Id}) - Key: {filter.Key}");
                                modeMismatch = true;
                                break;
                            }
                        }
                        if (modeMismatch) continue;
                    }

                    filtered.Add(lobby);
                }

                onComplete?.Invoke(filtered);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }

        // Overload with default count and optional callbacks
        public static IEnumerator QueryLobbies(bool includePrivate, UnityAction<List<Lobby>> onComplete, UnityAction<Exception> onError)
        {
            return QueryLobbies(20, includePrivate, onComplete, onError);
        }

        public static IEnumerator JoinLobby(Unity.Services.Lobbies.Models.Player player, string lobbyId, string password, UnityAction<Lobby> onComplete, UnityAction<Exception> onError)
        {
            if (string.IsNullOrEmpty(lobbyId))
            {
                onError?.Invoke(new ArgumentException("lobbyId is required"));
                yield break;
            }

            var joinOptions = new JoinLobbyByIdOptions
            {
                Player = player
            };

            if (!string.IsNullOrEmpty(password))
                joinOptions.Password = password;

            var task = LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                onError?.Invoke(task.Exception);
            }
            else
            {
                onComplete?.Invoke(task.Result);
            }
        }

        public static IEnumerator UpdatePlayerData(Lobby currentLobby, string playerId, string key, string value, UnityAction<Lobby> onComplete, UnityAction<Exception> onError)
        {
            if (currentLobby == null) { onError?.Invoke(new InvalidOperationException("No lobby")); yield break; }
            var playerData = new Dictionary<string, PlayerDataObject> { [key] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value) };
            var options = new UpdatePlayerOptions { Data = playerData };
            var task = LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, playerId, options);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) onError?.Invoke(task.Exception);
            else onComplete?.Invoke(task.Result);
        }

        public static IEnumerator RemoveLobbyDataKey(Lobby currentLobby, string key, UnityAction<Lobby> onComplete, UnityAction<Exception> onError)
        {
            if (currentLobby == null) { onError?.Invoke(new InvalidOperationException("No lobby")); yield break; }
            var data = new Dictionary<string, DataObject> { [key] = null };
            var task = LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions { Data = data });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) onError?.Invoke(task.Exception);
            else onComplete?.Invoke(task.Result);
        }

        public static IEnumerator QuickJoinOrCreate(Player player, string lobbyName, int maxPlayers, CreateLobbyOptions createOptions, UnityAction<Lobby> onComplete, UnityAction<Exception> onError, int totalCount = 20, string mode = "")
        {
            // Try query
            List<Lobby> found = null;
            yield return QueryLobbies(totalCount, false, l => found = l, e => onError?.Invoke(e));
            if (found != null && found.Count > 0)
            {
                // try join first available
                foreach (var candidate in found)
                {
                    bool joined = false;
                    Exception joinEx = null;
                    WitLogger.Log($"Attempting to join lobby: {candidate.Name} ({candidate.Id})");
                    yield return JoinLobby(player, candidate.Id, null, l => { onComplete?.Invoke(l); joined = true; }, e => joinEx = e);
                    WitLogger.Log(joined ? $"Successfully joined lobby: {candidate.Name} ({candidate.Id})" : $"Failed to join lobby: {candidate.Name} ({candidate.Id}) - {joinEx?.Message}");
                    if (joined) yield break; // success
                }
            }
            // none joined -> create
            WitLogger.Log("No available lobbies found, creating new lobby.");
            yield return CreateNewLobby(lobbyName, maxPlayers, createOptions, onComplete, onError);
        }

        #endregion


        #region MatchMaking

        /// <summary>
        /// Initiates the matchmaking process using UGS Matchmaker and automatically configures Unity Relay 
        /// for the resulting session.
        /// </summary>
        /// <param name="maxPlayers">The maximum number of players allowed in the match.</param>
        /// <param name="queueName">The name of the Matchmaker Queue configured in the UGS Dashboard.</param>
        /// <param name="ct">CancellationTokenSource for stopping the polling process.</param>
        /// <param name="onComplete">Callback providing the ISession object upon successful matchmaking.</param>
        /// <param name="onError">Callback for handling exceptions.</param>
        public static IEnumerator StartMatchMaking(CancellationTokenSource ct,
            UnityAction<ISession> onComplete,
            UnityAction<Exception> onError, string sessionType = "matchmade", ushort maxPlayers = 2, string queueName = "DefaultQueue")
        {
            var options = new MatchmakerOptions { QueueName = queueName };
            var sessionOptions = new SessionOptions();
            sessionOptions.WithRelayNetwork();
            sessionOptions.MaxPlayers = maxPlayers;
            sessionOptions.Type = sessionType;

            var task = MultiplayerService.Instance.MatchmakeSessionAsync(
                options,
                sessionOptions,
                ct.Token
            );

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                onError?.Invoke(task.Exception);
            }
            else
            {
                // --- IMPORTANT NOTE FOR USER ---
                /* 
                 * When this task completes successfully, the ISession is returned, 
                 * AND the UnityTransport component in your scene is automatically configured 
                 * by the Multiplayer Service SDK with the correct Relay data (Host or Client settings).
                 * 
                 * The next step is simply to call NetworkManager.Singleton.StartClient() 
                 * in your onComplete callback method to begin the network connection.
                 * The transport internally handles starting as the Host if it was designated as such.
                */
                // -------------------------------
                onComplete?.Invoke(task.Result);
            }

            yield return null;
        }

        #endregion

        public static IEnumerator CreateRelayHost(
            int maxPlayers,
            UnityAction<Allocation> onComplete,
            UnityAction<Exception> onError)
        {
            var task = RelayService.Instance.CreateAllocationAsync(maxPlayers);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
                onError?.Invoke(task.Exception);
            else
                onComplete?.Invoke(task.Result);
        }

        public static IEnumerator JoinRelay(
            string joinCode,
            UnityAction<JoinAllocation> onComplete,
            UnityAction<Exception> onError)
        {
            var task = RelayService.Instance.JoinAllocationAsync(joinCode);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
                onError?.Invoke(task.Exception);
            else
                onComplete?.Invoke(task.Result);
        }

        public static void StartRelayServer(Allocation allocation)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }

    }
}