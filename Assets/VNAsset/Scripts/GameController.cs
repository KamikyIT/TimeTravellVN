using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour {

	IEnumerator Start () {
		yield return new WaitForSeconds(1);

		ScriptController.instance.GotoScene("@start");
	}
}
