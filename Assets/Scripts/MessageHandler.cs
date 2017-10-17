using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
	public class MessageHandler: MonoBehaviour
	{
		public InputField inputMessage;
		public NetworkHandler netHand;

		public void OnSend()
		{
			string message = inputMessage.text.Trim();
			if(message != "")
			{
				netHand.OnUserSendMessage(message);
				inputMessage.text = "";
			}
		}

		public void Ready()
		{
			inputMessage.interactable = true;
		}
	}
}
