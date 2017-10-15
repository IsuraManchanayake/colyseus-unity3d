using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
	public class JoinCreateHandler : MonoBehaviour
	{
		public NetworkHandler netHand;
		public InputField inputPrivateGameID;
		public InputField inputName;

		private void Start()
		{
			inputName.text = "Hello" + Random.Range(0, 1000).ToString("D3");
		}

		public void OnJoinPublic(Button button)
		{
			string name = inputName.text.Trim();
			if (name == "")
			{
				return;
			}
			print("trying to join public game");
			Dictionary<string, object> options = new Dictionary<string, object>
			{
				{"name", name},
				{"isPrivate", false}
			};
			button.interactable = false;
			StartCoroutine(netHand.Join(options));
		}

		public void OnJoinPrivate(Button button)
		{
			string name = inputName.text.Trim();
			string privateGameID = inputPrivateGameID.text.Trim();
			if (privateGameID == "" || name == "")
			{
				return;
			}
			print("trying to join private game");
			Dictionary<string, object> options = new Dictionary<string, object>
			{
				{"name", name},
				{"isPrivate", false},
				{"gameID", privateGameID}
			};
			button.interactable = false;
			StartCoroutine(netHand.Join(options));
		}

		public void OnCreatePrivate(Button buton)
		{
			string name = inputName.text.Trim();
			if (name == "")
			{
				return;
			}
			print("trying to create private game");
			Dictionary<string, object> options = new Dictionary<string, object>
			{
				{"name", name},
				{"isPrivate", false}
			};
			buton.interactable = false;
			StartCoroutine(netHand.Join(options));
		}
	}
}
