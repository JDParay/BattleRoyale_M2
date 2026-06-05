using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections.Generic;

public class Scoreboard : MonoBehaviour
{
    [System.Serializable]
    public struct ScoreboardColumn
    {
        public TMP_Text rankText;
        public TMP_Text nameText;
        public TMP_Text scoreText;
    }

    [Header("Scoreboard Layout")]
    public List<ScoreboardColumn> columns;

    private void Start()
    {
        // Clear placeholder text on startup
        foreach (var col in columns)
        {
            col.nameText.text = "---";
            col.scoreText.text = "(0)";
        }
        UpdateScoreboard();
    }

    public void UpdateScoreboard()
    {
        var sortedPlayers = PhotonNetwork.PlayerList
            .OrderByDescending(p => p.CustomProperties.TryGetValue("Score", out object s) ? (int)s : 0)
            .ToList();

        for (int i = 0; i < columns.Count; i++)
        {
            if (i < sortedPlayers.Count)
            {
                Player player = sortedPlayers[i];
                int score = player.CustomProperties.TryGetValue("Score", out object s) ? (int)s : 0;

                columns[i].nameText.text = player.NickName;
                columns[i].scoreText.text = $"({score})";
                
                // Keep the column elements visible if a player occupies the slot
                columns[i].rankText.gameObject.SetActive(true);
                columns[i].nameText.gameObject.SetActive(true);
                columns[i].scoreText.gameObject.SetActive(true);
            }
            else
            {
                // Hide or clear elements if there aren't enough players in the match
                columns[i].nameText.text = "";
                columns[i].scoreText.text = "";
                columns[i].rankText.gameObject.SetActive(false); 
            }
        }
    }
}