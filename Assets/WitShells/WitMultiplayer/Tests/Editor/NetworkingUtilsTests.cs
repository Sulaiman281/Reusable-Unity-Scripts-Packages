using System;
using NUnit.Framework;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using WitShells.WitMultiplayer;

namespace WitShells.WitMultiplayer.Tests.Editor
{
    public class NetworkingUtilsTests
    {
        [Test]
        public void UpdateLobby_ThrowsWhenLobbyIsNull()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                NetworkingUtils.UpdateLobby(null, "testKey", null, null, null);
            });
        }

        [Test]
        public void RemoveLobbyDataKey_ThrowsWhenLobbyIsNull()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                NetworkingUtils.RemoveLobbyDataKey(null, "testKey", null, null);
            });
        }

        [Test]
        public void LobbyOptions_SetsFieldsCorrectly_WhenDataIsNull()
        {
            var player = NetworkingUtils.GetNewPlayer("player123");

            var options = NetworkingUtils.LobbyOptions(player, "secret", true, null);

            Assert.IsNotNull(options);
            Assert.AreEqual(player, options.Player);
            Assert.AreEqual("secret", options.Password);
            Assert.IsTrue(options.IsPrivate);
            Assert.IsNotNull(options.Data);
            Assert.IsEmpty(options.Data);
        }
    }
}
