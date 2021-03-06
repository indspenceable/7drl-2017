﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearUI : MonoBehaviour {
	[SerializeField]
	int slot;
	[SerializeField]
	UnityEngine.UI.Text itemName;
	[SerializeField]
	UnityEngine.UI.Text cooldown;
	[SerializeField]
	UnityEngine.UI.Text chargesTotal;
	[SerializeField]
	UnityEngine.UI.Text chargesThisLevel;

	public bool hover;
	public void PointerEnter() {
		hover = true;
	}
	public void PointerExit() {
		hover = false;
	}
	private Player player;

	public void InstallPlayer(Player p) {
		player = p;
	}

	public string ItemName() {
		return player.GetItem(slot).DisplayName;
	}
	public string ItemDesc() {
		return player.GetItem(slot).Description;
	}


	// Update is called once per frame
	void Update () {
		// Actually draw the UI!
		if (player == null) return;
		Item item = player.GetItem(slot);
		itemName.text = item.DisplayName;
		if (item.itemType.cooldown != -1) {
			cooldown.text = "Cooldown: " + item.cooldown + " / " + item.itemType.cooldown;
		} else {
			cooldown.text ="";
		}
		if (item.itemType.totalCharges != -1) {
			chargesTotal.text = "" + item.chargesUsed + " / " + item.itemType.totalCharges + " used.";
		} else {
			chargesTotal.text ="";
		}

		if (item.itemType.levelLimit != -1 ) {
			chargesThisLevel.text = "" + item.usesThisLevel + " / " + item.itemType.levelLimit + " this level";
		} else {
			chargesThisLevel.text ="";
		}
	}
}
