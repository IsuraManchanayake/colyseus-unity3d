using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Colyseus;
using GameDevWare.Serialization;
using UnityEngine.UI;

namespace Assets.Scripts
{
	public class NetworkHandler : MonoBehaviour
	{
		public string host = "localhost";
		public string port = "3553";

		private string roomName = "chat";

		internal Client client;
		internal Room gameRoom;

		public GameObject panelJoinCreate;
		public GameObject panelSendMessage;
		public GameObject playerPrefab;
		public Transform playerParentTransform;

		#region Player Information
		private PlayerController localPlayer;
		private PlayerController remotePlayer;
		#endregion

		internal IEnumerator Join(Dictionary<string, object> options)
		{
			print("options");
			options.ToList().ForEach(x => print(x.Key + " -> " + x.Value));

			string uri = "ws://" + host + ":" + port;
			client = new Client(uri);
			client.OnOpen += OnOpenHandler;

			yield return StartCoroutine(client.Connect());

			gameRoom = client.Join(roomName, options);
			gameRoom.OnReadyToConnect += (sender, e) => StartCoroutine(gameRoom.Connect());
			gameRoom.OnJoin += OnRoomJoined;
			gameRoom.OnUpdate += OnUpdateHandler;
			gameRoom.OnLeave += OnLeaveRoom;
			gameRoom.OnData += OnData;

			client.OnError += OnError;

			gameRoom.Listen("players/:id/:axis", OnMove);
			gameRoom.Listen("players/:id/color", OnColor);
			gameRoom.Listen("players/:id", OnPlayerChange);

			while (true)
			{
				client.Recv();
				if (client.error != null)
				{
					print("Error: " + client.error);
					OnNetworkError();
					break;
				}
				yield return null;
			}

			OnApplicationQuit();
		}

		private void OnPlayerChange(DataChange obj)
		{
			print("player change");
		}

		#region GamePlay
		#region UserActions
		internal void OnUserChangeColor(int color)
		{
			print("User changed color to " + color);
			Dictionary<string, object> data = new Dictionary<string, object>
			{
				{"type", "color"},
				{"color", color}
			};
			gameRoom.Send(data);
		}

		internal void OnUserSendMessage(string message)
		{
			print("User send " + message);
			Dictionary<string, object> data = new Dictionary<string, object>
			{
				{"type", "message"},
				{"message", message}
			};
			gameRoom.Send(data);
		}

		internal void OnUserMove(Vector3 position)
		{
			print("User change position " + position);
			Dictionary<string, object> data = new Dictionary<string, object>
			{
				{"type", "move"},
				{"x", position.x},
				{"y", position.y}
			};
			gameRoom.Send(data);
		}
		#endregion

		#region ServerListen
		public void OnColor(DataChange obj)
		{
			print("On color");
			GetPlayerByID(obj.path["id"]).SetColor(int.Parse(obj.value.ToString()));
		}

		public void OnMove(DataChange obj)
		{
			print("On Move");
			Vector3 delta = Vector3.zero;
			PlayerController player = GetPlayerByID(obj.path["id"]);
			if (obj.path["axis"] == "x")
			{
				delta.x = float.Parse(obj.value.ToString()) - player.gameObject.transform.localPosition.x;
			}
			else
			{
				delta.y = float.Parse(obj.value.ToString()) - player.gameObject.transform.localPosition.y;
			}
			player.gameObject.transform.localPosition += delta;
		}
		#endregion
		#endregion

		private void OnApplicationQuit()
		{
			if (client != null)
			{
				client.Close();
			}
			if (gameRoom != null)
			{
				gameRoom.Leave();
			}
		}

		private void OnNetworkError()
		{
			print("Network error");
			if (client != null)
			{
				client.Close();
			}
		}

		private void OnError(object sender, EventArgs e)
		{
			print("Error");
		}

		private void OnData(object sender, MessageEventArgs e)
		{
			print("message recieved");
			IndexedDictionary<string, object> data = (IndexedDictionary<string, object>)e.data;
			data.ToList().ForEach(x => print(x.Key + " -> " + x.Value));
			if (data.ContainsKey("command"))
			{
				switch (data["command"].ToString())
				{
					case "playerData":
						{
							float x = float.Parse(data["x"].ToString());
							float y = float.Parse(data["y"].ToString());
							int color = int.Parse(data["color"].ToString());
							string name = data["name"].ToString();
							PlayerController playerController = Instantiate(playerPrefab
								, new Vector3(x, y, 0f)
								, Quaternion.identity
								, playerParentTransform)
								.GetComponent<PlayerController>();
							playerController.netHand = this;
							playerController.SetColor(color);
							playerController.SetName(name);
							
							if (data["id"].ToString() != client.id)
							{
								localPlayer = playerController;
							}
							else
							{
								remotePlayer = playerController;
								remotePlayer.interactable = false;
							}
						}
						break;
					case "message":
						{
							string message = data["message"].ToString();
							GetPlayerByID(data["id"].ToString()).AddMessage(message);
						}
						break;
				}
			}
		}

		private void OnLeaveRoom(object sender, EventArgs e)
		{
			print("Left room");
		}

		private void OnUpdateHandler(object sender, RoomUpdateEventArgs e)
		{
			print("OnUpdate");
		}

		private void OnRoomJoined(object sender, EventArgs e)
		{
			print("Joined room successfully");
			panelJoinCreate.SetActive(false);
			panelSendMessage.SetActive(true);
		}

		private void OnOpenHandler(object sender, EventArgs e)
		{
			print("Connected to server. Client id: " + client.id);
		}

		private PlayerController GetPlayerByID(string id)
		{
			return id == client.id ? localPlayer : remotePlayer;
		}
	}
}
