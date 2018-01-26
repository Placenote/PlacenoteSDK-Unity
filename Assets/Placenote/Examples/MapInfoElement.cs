using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapInfoElement : MonoBehaviour
{
	[SerializeField] Text mMapIdText;
	[SerializeField] Toggle mToggle;

	public void Initialize (LibPlacenote.MapInfo mapInfo, ToggleGroup toggleGroup,
	                       RectTransform listParent, UnityAction<bool> onToggleChanged)
	{
		mMapIdText.text = mapInfo.placeId;
		mToggle.group = toggleGroup;
		gameObject.transform.SetParent (listParent);
		mToggle.onValueChanged.AddListener (onToggleChanged);
	}
}
