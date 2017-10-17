using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using Colyseus;
using GameDevWare.Serialization;
using UnityEngine.UI;

namespace Assets.Scripts
{
	public class NetworkHandler : MonoBehaviour
	{
		/// <summary>
		/// Host IP address
		/// </summary>
		public string host = "localhost";

		/// <summary>
		/// Port to listen
		/// </summary>
		public string port = "3553";

		/// <summary>
		/// A game room is identified by room names. An individual client may connect to more than one room.
		/// The name is specified in the server.
		/// </summary>
		private string roomName = "chat";

		/// <summary>
		/// Client object
		/// </summary>
		internal Client client;

		/// <summary>
		/// Room object
		/// </summary>
		internal Room gameRoom;

		public GameObject panelJoinCreate;
		public GameObject panelSendMessage;
		public GameObject playerPrefab;
		public Text textHelper;
		public Transform playerParentTransform;

		public MessageHandler messageHand;

		private PlayerController localPlayer;
		private PlayerController remotePlayer;

		#region Joining

		/// <summary>
		/// Connects to the server, then connects to the room and starts listening to the server
		/// </summary>
		/// <param name="options">
		/// Additional information when connecting to the server. options["type"] field is mandatory. For this occasion,
		/// options["type"] is one of joinPublic, joinPrivate or createPrivate. options can be used for many applications like
		///	password protection, matching players by their ranks etc.
		/// </param>
		/// <returns></returns>
		internal IEnumerator Join(Dictionary<string, object> options)
		{
			print("options");
			options.ToList().ForEach(x => print(x.Key + " -> " + x.Value));

			string uri = "ws://" + host + ":" + port;
			client = new Client(uri);

			client.OnOpen += OnOpenHandler;
			client.OnError += OnError;

			yield return StartCoroutine(client.Connect());

			gameRoom = client.Join(roomName, options);
			gameRoom.OnReadyToConnect += (sender, e) => StartCoroutine(gameRoom.Connect());
			gameRoom.OnJoin += OnRoomJoined;
			gameRoom.OnUpdate += OnUpdateHandler;
			gameRoom.OnLeave += OnLeaveRoom;
			gameRoom.OnData += OnData;

			// Starts listening the server
			StartCoroutine(ListenServer());
		}

		/// <summary>
		/// Triggers when successfully joined the room
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnRoomJoined(object sender, EventArgs e)
		{
			print("Joined room successfully");
			panelJoinCreate.SetActive(false);
			panelSendMessage.SetActive(true);
		}

		/// <summary>
		/// Triggers when successfully joined the server
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOpenHandler(object sender, EventArgs e)
		{
			print("Connected to server. Client id: " + client.id);
		}

		#endregion

		#region GamePlay
		#region UserActions

		/// <summary>
		/// Triggers when the local player changes his color while gampe play
		/// </summary>
		/// <param name="color">Color of the player</param>
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

		/// <summary>
		/// Triggers when the local player sends a message while game play
		/// </summary>
		/// <param name="message">The message</param>
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

		/// <summary>
		/// Triggers when the local player changes his position while game play
		/// </summary>
		/// <param name="position">The position vector</param>
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

		/// <summary>
		/// Triggers when server player changes his color while game play
		/// </summary>
		public void OnColor(DataChange obj)
		{
			print("On color");
			/**
			 * See <see cref="ListenServer"/>. OnColor is triggered when "players/:id/color" changes its' value.
			 * obj.path contains the unknown :id value of the changed path. obj.value contains the changed value while
			 * obj.operation can has values add, replace and remove. 
			 */
			GetPlayerByID(obj.path["id"]).SetColor(int.Parse(obj.value.ToString()));
		}

		/// <summary>
		/// Triggers when server player changes his position while game play
		/// </summary>
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

		#region Error handling

		/// <summary>
		/// Triggers when an error occur in the network
		/// </summary>
		private void OnNetworkError()
		{
			print("Network error");
			SceneManager.LoadScene("ExampleScene");
		}

		/// <summary>
		/// Triggers usually when join request fails
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnError(object sender, EventArgs e)
		{
			print("Error");
			SceneManager.LoadScene("ExampleScene");
		}

		#endregion

		#region Recieving data from the server

		/// <summary>
		/// Listening to server while the connection prevails
		/// </summary>
		/// <returns></returns>
		internal IEnumerator ListenServer()
		{
			/** The game room on the server has its' state variable. For this example while the game on going, the state variable
			 * looks like
			 *		state = {
			 *			players: {
			 *				{
			 *					'aAbB12345': {
			 *						id: 'aAbB12345',
			 *						name: 'hello1',
			 *						x: 1,
			 *						y: -1.2,
			 *						color: 0
			 *					}
			 *				},
			 *				{
			 *					'12345aAbB': {
			 *						id: '12345aAbB',
			 *						name: 'hello2',
			 *						x: 1.4,
			 *						y: -1,
			 *						color: 1
			 *					}
			 *				}
			 *			},
			 *			pwd: '1234aAbB'
			 *		}
			 *		
			 *		One of the core ideas of Colyseus server is to listen changes of the variables by their path in the state variables.
			 *	For example, if you want to listen changes of the color of the state.pwd, you need to specify the path by
			 *	gameRoom.Listen("pwd", SomeMethod1). To listen for any changes of color of the players, the path should be "players/:id/color".
			 *	Names like :id starts with a colon are called the placeholders. Read more on placeholders in
			 *	https://github.com/endel/delta-listener. OnMove is triggered by any change in the players["something"].x or
			 *	players["something"].y. A change is triggered when the variable is added, replaced or removed.
			 */
			gameRoom.Listen("players/:id/:axis", OnMove);
			gameRoom.Listen("players/:id/color", OnColor);

			while (true)
			{
				client.Recv();

				// Network errors or errors in the server itself causes client.error
				if (client.error != null)
				{
					print("Error: " + client.error);
					OnNetworkError();
					break;
				}
				yield return null;
			}
		}

		/// <summary>
		/// Triggers when the server sends a messasge. The message is obtained through e.data which is an IndexedDictionary<string, object>.
		/// For this example, data["command"] specifies the type of the command.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnData(object sender, MessageEventArgs e)
		{
			print("message recieved");
			IndexedDictionary<string, object> data = (IndexedDictionary<string, object>)e.data;
			data.ToList().ForEach(x => print(x.Key + " -> " + x.Value));
			if (data.ContainsKey("command"))
			{
				switch (data["command"].ToString())
				{
					// For this example project, player data is sent when the server is ready to start the game
					case "playerData":
						{
							// Server always send primitives as strings so variables should be parsed
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
							textHelper.text = "";
							messageHand.Ready();
						}
						break;
					case "message":
						{
							string message = data["message"].ToString();
							GetPlayerByID(data["id"].ToString()).AddMessage(message);
						}
						break;
					case "oppLeft":
						{
							OnApplicationQuit();
							SceneManager.LoadScene("ExampleScene");
						}
						break;
					// For this example, server sends the private game ID to the client who created the private game for other people
					//		to connect the game
					case "privateGameID":
						{
							textHelper.text = "Private Game ID is " + data["privateGameID"] + " and it is copied to clipboard";
							TextEditor textEditor = new TextEditor
							{
								text = data["privateGameID"].ToString()
							};
							textEditor.SelectAll();
							textEditor.Copy();
						}
						break;
				}
			}
		}

		/// <summary>
		/// Triggers whenever the state variable is updated
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnUpdateHandler(object sender, RoomUpdateEventArgs e)
		{
			print("OnUpdate");
		}

		#endregion

		#region Leaving
		/// <summary>
		/// Leaving the game room and closing the connection when quitting the game
		/// </summary>
		private void OnApplicationQuit()
		{
			if (gameRoom != null)
			{
				gameRoom.Leave();
			}
			if (client != null)
			{
				client.Close();
			}
		}

		/// <summary>
		/// Triggers when the local player leaves
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnLeaveRoom(object sender, EventArgs e)
		{
			print("Left room");
		}
		#endregion

		#region Additional

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns>Player controller by the client id</returns>
		private PlayerController GetPlayerByID(string id)
		{
			return id == client.id ? localPlayer : remotePlayer;
		}

		#endregion
	}
}
