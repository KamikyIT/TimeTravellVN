using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionButton : MonoBehaviour {

	[SerializeField] Text buttonText;
	[SerializeField] Button button;

	Action clickCallback;

	void Awake()
	{
		button.onClick.AddListener(() => clickCallback?.Invoke());
	}

	public void Refresh(string text, Action clickCallback)
	{
		buttonText.text = text;

		this.clickCallback = clickCallback;
	}
}
