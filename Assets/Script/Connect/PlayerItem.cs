using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
    [SerializeField] Image avatarImage;
    [SerializeField] TMP_Text playerName;
    [SerializeField] Sprite[] avatarSprites;

    public void Set(Photon.Realtime.Player player) {
        if (player.CustomProperties.TryGetValue(PropertyNames.Player.AvatarIndex, out var value)) {
            avatarImage.sprite = avatarSprites[(int)value];
        }

        playerName.text = player.NickName;

        if (player == PhotonNetwork.MasterClient) {
            playerName.text += "<b> (Master) </b>";
            playerName.color = Color.red;
        }

        if (player.IsLocal && !player.IsMasterClient) {
            playerName.text += "<b> (You) </b>";
            playerName.color = Color.yellow;
        }
    }
}