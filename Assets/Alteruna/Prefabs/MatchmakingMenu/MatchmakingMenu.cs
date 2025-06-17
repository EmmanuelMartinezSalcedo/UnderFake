using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Alteruna.Trinity;
using UnityEngine.Serialization;

namespace Alteruna
{
	public class MatchmakingMenu : CommunicationBridge
	{
		public enum GameState
		{
			Started,
			Searching,
			Filling,
			Full,
			Playing,
		}

		[NonSerialized] public GameState State;

		public Text ConnectedIndicator;
		public Text ConnectedPlayersText;
		public GameObject MatchmakingButton;
		public GameObject ForceStartButton;
		public GameObject StartButton;
		public GameObject ReadyButton;

		public GameObject SearchingText;
		public GameObject WaitingText;
		public Text ConnectingText;
		public GameObject PlayersInfoText;

		public GameObject Content;
		public GameObject EntryPrefab;

		[FormerlySerializedAs("GameStarted")] public UnityEvent<Multiplayer, int> OnGameStart;

		[HideInInspector] public List<MatchmakingEntry> PlayerEntries = new List<MatchmakingEntry>();
		[HideInInspector] public bool IsOwner;
		[HideInInspector] public bool IsReady;
		[HideInInspector] public int ConnectedUsers = 0;


		public void Start()
		{
			State = GameState.Started;
			ResetMenu();

			if (Multiplayer != null)
			{
				Multiplayer.OnDisconnected.AddListener(OnDisconnected);
				Multiplayer.OnConnected.AddListener(OnConnected);
				Multiplayer.OnRoomJoined.AddListener(OnJoined);
				Multiplayer.OnRoomLeft.AddListener(OnLeft);
				Multiplayer.OnOtherUserJoined.AddListener(OnOtherJoined);
				Multiplayer.OnOtherUserLeft.AddListener(OnOtherLeft);

				Multiplayer.RegisterRemoteProcedure("MarkReady", RemoteMarkedReady);
				Multiplayer.RegisterRemoteProcedure("StartGame", RemoteStartedGame);

				if (!Multiplayer.enabled)
				{
					ConnectingText.text = "Offline";
				}

				InvokeRepeating(nameof(CheckForBlock), 3, 5);
			}
		}

		public void JoinMatchmaking()
		{
			if (Multiplayer.IsConnected)
			{
				Multiplayer.JoinMatchmaking();
				State = GameState.Searching;
				SearchingText.gameObject.SetActive(true);
				MatchmakingButton.SetActive(false);
			}
		}

		public void StartIfAllReady()
		{
			if (!IsOwner)
			{
				return;
			}

			for (int i = 0, l = PlayerEntries.Count; i < l; i++)
			{
				if (!PlayerEntries[i].Ready)
				{
					return;
				}
			}

			StartGame();
		}

		public void ForceStart()
		{
			if (!IsOwner)
			{
				return;
			}

			if (IsOwner)
			{
				// TODO: Set updateState on room
				//_room?.UpdateState(SessionState.Busy);
				Multiplayer.LockRoom();
			}

			StartGame();
		}

		public void StartGame()
		{
			if (IsOwner)
			{
				Multiplayer.InvokeRemoteProcedure("StartGame", (ushort)UserId.All);
				Multiplayer.LockRoom();
			}

			OnGameStart.Invoke(Multiplayer, ConnectedUsers);
			State = GameState.Playing;
			gameObject.SetActive(false);
		}

		private void RemoteStartedGame(ushort fromUser, ProcedureParameters p, uint callID, ITransportStreamReader reader)
		{
			StartGame();
		}

		private void RemoteMarkedReady(ushort fromUser, ProcedureParameters p, uint callID, ITransportStreamReader reader)
		{
			int len = PlayerEntries.Count;
			for (int i = 0; i < len; i++)
			{
				if (PlayerEntries[i].Id == fromUser)
				{
					bool ready;
					p.Get("isReady", out ready);
					PlayerEntries[i].SetReady(ready);
				}
			}
		}

		public void ToggleReady()
		{
			IsReady = !IsReady;
			PlayerEntries[0].SetReady(IsReady);
			ProcedureParameters parameters = new ProcedureParameters();
			parameters.Set("isReady", IsReady);
			Multiplayer.InvokeRemoteProcedure("MarkReady", (ushort)UserId.All, parameters);
		}

		public void ResetMenu()
		{
			State = GameState.Started;
			StartButton.SetActive(false);
			ForceStartButton.SetActive(false);
			ReadyButton.SetActive(false);
			SearchingText.SetActive(false);
			WaitingText.SetActive(false);
			ConnectingText.enabled = true;
			PlayersInfoText.SetActive(false);

			int len = PlayerEntries.Count;
			for (int i = 0; i < len; i++)
			{
				Destroy(PlayerEntries[i].gameObject);
			}

			PlayerEntries.Clear();
		}

		public void OnConnected(Multiplayer multiplayer, Endpoint endpoint)
		{
			ConnectedIndicator.text = "Connected";
			ConnectedIndicator.color = Color.green;
			ConnectingText.enabled = false;
			MatchmakingButton.SetActive(true);
		}

		public void OnDisconnected(Multiplayer multiplayer, Endpoint endpoint)
		{
			ConnectedIndicator.text = "Disconnected";
			ConnectedIndicator.color = Color.gray;

			ConnectedUsers = 0;
			UpdateConnectedPlayersText();

			ResetMenu();
		}

		public void OnJoined(Multiplayer multiplayer, Room room, User user)
		{
			State = GameState.Filling;

			bool isOwner = user.Index == multiplayer.LowestUserIndex;
			if (!isOwner)
			{
				WaitingText.SetActive(true);
			}

			SetOwner(isOwner);

			ReadyButton.SetActive(true);
			SearchingText.SetActive(false);
			PlayersInfoText.SetActive(true);

			ConnectedUsers++;
			UpdateConnectedPlayersText();
			AddPlayerEntry(multiplayer.Me);
		}

		public void OnLeft(Multiplayer multiplayer)
		{
			ConnectedUsers = 0;
			UpdateConnectedPlayersText();
		}

		public void OnOtherJoined(Multiplayer multiplayer, User user)
		{
			ConnectedUsers++;
			UpdateConnectedPlayersText();

			AddPlayerEntry(user);

			SetOwner(user.Index == multiplayer.LowestUserIndex);
		}

		public void OnOtherLeft(Multiplayer multiplayer, User user)
		{
			ConnectedUsers--;
			UpdateConnectedPlayersText();

			RemovePlayerEntry(user.Index);
		}

		private int updateCount = 0;

		void CheckForBlock()
		{
			updateCount++;

			if (!Multiplayer.enabled)
			{
				ConnectingText.text = "Offline";
			}
			else if (!Multiplayer.IsConnected)
			{
				switch (updateCount % 4)
				{
					case 0:
						ConnectingText.text = "Connecting.   ";
						break;
					case 1:
						ConnectingText.text = "Connecting..  ";
						break;
					case 2:
						ConnectingText.text = "Connecting... ";
						break;
					default:
						ConnectingText.text = "Connecting....";
						break;
				}
			}

			if (ConnectedIndicator.text != "Connected" && ConnectedIndicator.text != "Disconnected")
			{
				return;
			}

			ResponseCode blockedReason = Multiplayer.GetLastBlockResponse();
			if (blockedReason == ResponseCode.NaN) return;

			string str = blockedReason.ToString();
			str = string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
			ConnectedIndicator.text = str;
			ConnectedIndicator.color = Color.red;
		}

		void SetOwner(bool isOwner = true)
		{
			if (isOwner == IsOwner)
			{
				return;
			}

			IsOwner = isOwner;
			if (isOwner)
			{
				StartButton.SetActive(true);
				ForceStartButton.SetActive(true);
				WaitingText.SetActive(false);
			}
			else
			{
				StartButton.SetActive(false);
				ForceStartButton.SetActive(false);
				WaitingText.SetActive(true);
			}
		}

		void UpdateConnectedPlayersText()
		{
			ConnectedPlayersText.text = "(" + ConnectedUsers + "/" + Multiplayer.CurrentRoom.MaxUsers + ")";
		}

		void RemovePlayerEntry(ushort id)
		{
			int len = PlayerEntries.Count;
			for (int i = 0; i < len; i++)
			{
				if (PlayerEntries[i].Id == id)
				{
					GameObject entry = PlayerEntries[i].gameObject;
					PlayerEntries.Remove(PlayerEntries[i]);
					Destroy(entry);
				}
			}
		}

		void AddPlayerEntry(User user)
		{
			GameObject entry = Instantiate(EntryPrefab, Content.transform);
			entry.SetActive(true);
			MatchmakingEntry player = entry.GetComponentInChildren<MatchmakingEntry>();
			player.NameText.text = user.Name;
			player.Id = user.Index;
			player.SetReady(false);

			if (user.Index == 0)
			{
				player.SetOwner(true);
			}
			else
			{
				player.SetOwner(false);
			}

			PlayerEntries.Add(player);

			if (ConnectedUsers >= Multiplayer.CurrentRoom.MaxUsers)
			{
				State = GameState.Full;
			}
		}
		
		public new void Reset()
		{
			base.Reset();
			EnsureEventSystem.Ensure(true);
		}
	}
}