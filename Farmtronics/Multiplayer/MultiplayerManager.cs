using System.Collections.Generic;
using Farmtronics.M1.Filesystem;
using Farmtronics.Multiplayer.Messages;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Farmtronics.Multiplayer {
	static class MultiplayerManager {
		internal static long hostID;
		internal static Dictionary<long, RealFileDisk> remoteDisks = new();
		
		public static void OnPeerContextReceived(object sender, PeerContextReceivedEventArgs e) {
			if (e.Peer.GetMod(ModEntry.instance.ModManifest.UniqueID) == null) {
				ModEntry.instance.Monitor.Log($"Couldn't find Farmtronics for player {e.Peer.PlayerID}. Make sure the mod is correctly installed.", LogLevel.Error);
			} else if (e.Peer.IsHost) {
				ModEntry.instance.Monitor.Log($"Found host player ID: {e.Peer.PlayerID}");
				hostID = e.Peer.PlayerID;
			}
		}
		
		public static void OnPeerConnected(object sender, PeerConnectedEventArgs e) {
			if (Context.IsMainPlayer) {
				SaveData.CreateUsrDisk(e.Peer.PlayerID);
				var disk = new RealFileDisk(SaveData.GetUsrDiskPath(e.Peer.PlayerID));
				disk.readOnly = false;
				remoteDisks.Add(e.Peer.PlayerID, disk);
			}
			else if (e.Peer.IsHost) hostID = e.Peer.PlayerID;
		}
		
		public static void OnPeerDisconnected(object sender, PeerDisconnectedEventArgs e) {
			if (Context.IsMainPlayer) remoteDisks.Remove(e.Peer.PlayerID);
		}

		public static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e) {
			if (e.FromModID != ModEntry.instance.ModManifest.UniqueID) return;
			
			ModEntry.instance.Monitor.Log($"Receiving message from '{e.FromPlayerID}' of type: {e.Type}");

			switch (e.Type) {
			case nameof(AddBotInstance):
				AddBotInstance addBotInstance = e.ReadAs<AddBotInstance>();
				addBotInstance.Apply();
				return;
			default:
				ModEntry.instance.Monitor.Log($"Couldn't receive message of unknown type: {e.Type}", LogLevel.Error);
				return;
			}
		}

		public static void SendMessage<T>(T message, long[] playerIDs = null) where T : BaseMessage<T> {
			if (!Context.IsMultiplayer) return;
			
			if (playerIDs != null) ModEntry.instance.Monitor.Log($"Sending message: {message} to {string.Join(',', playerIDs)}");
			else ModEntry.instance.Monitor.Log($"Broadcasting message: {message}");
			ModEntry.instance.Helper.Multiplayer.SendMessage(message, typeof(T).Name, modIDs: new[] { ModEntry.instance.ModManifest.UniqueID }, playerIDs: playerIDs);
		}
		
		public static void SendMessageToHost<T>(T message) where T : BaseMessage<T> {
			SendMessage(message, new[] { hostID });
		}
	}
}