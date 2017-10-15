using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
	public class PlayerController : MonoBehaviour
	{
		public NetworkHandler netHand;
		internal bool interactable = true;

		#region Messages

		public Text textMessage;

		public void AddMessage(string message)
		{
			if (textMessage.text.Length > 200)
			{
				textMessage.text = "";
			}
			textMessage.text += message + "\n";
		}
		#endregion

		#region Color

		public GameObject goButton;
		private int color = 1;

		public void OnClickButton()
		{
			if (interactable)
			{
				color = 1 - color;
				SetColor(color);
				netHand.OnUserChangeColor(color);
			}
		}

		public void SetColor(int color)
		{
			this.color = color;
			goButton.GetComponent<Image>().color = new Color(color, color, color);
		}
		#endregion

		#region Name

		public Text textName;

		public void SetName(string name)
		{
			textName.text = name;
		}
		#endregion

		#region Drag and Drop

		private Vector3 screenPoint;
		private Vector3 offset;

		private void OnMouseDown()
		{
			if (interactable)
			{
				screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
				offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(
					new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z)
				);
			}
		}

		private void OnMouseDrag()
		{
			if (interactable)
			{
				Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
				transform.position = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
				netHand.OnUserMove(gameObject.transform.localPosition);
			}
		}
		#endregion
	}
}
