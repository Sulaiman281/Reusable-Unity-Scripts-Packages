using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using WitShells.DesignPatterns;

namespace WitShells.WitMultiplayer
{
    public class LobbyManager : LobbyEventCallbacks
    {
        private Lobby lobby;
        private CancellationTokenSource _heartBeatCts;

        public int AvailableSlots
        {
            get
            {
                return lobby.AvailableSlots;
            }
        }

        public List<Player> Players
        {
            get
            {
                return lobby.Players;
            }
        }

        public int MaxPlayers
        {
            get
            {
                return lobby.MaxPlayers;
            }
        }

        public Lobby Lobby => lobby;

        public LobbyManager(Lobby lobby)
        {
            this.lobby = lobby;
            _ = LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
        }

        public Lobby GetLobby()
        {
            return lobby;
        }

        public bool IsHost(string playerId)
        {
            return string.Equals(lobby.HostId, playerId, StringComparison.OrdinalIgnoreCase);
        }

        public void Leave(string playerId)
        {
            _ = LobbyService.Instance.RemovePlayerAsync(lobby.Id, playerId);
        }

        public void StartHostHeartBeat()
        {
            _heartBeatCts = new CancellationTokenSource();
            _ = HostHeartBeatLoopAsync(lobby.Id, _heartBeatCts.Token);
        }

        public void StopHostHeartBeat()
        {
            if (_heartBeatCts != null)
            {
                _heartBeatCts.Cancel();
                _heartBeatCts.Dispose();
                _heartBeatCts = null;
            }
        }

        private async Task HostHeartBeatLoopAsync(string lobbyId, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                    await Task.Delay(15000, ct);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Error in HostHeartBeatLoopAsync: {ex.Message}");
            }
        }
    }
}