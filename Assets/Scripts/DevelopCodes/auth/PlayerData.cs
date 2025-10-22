using System.Collections;
using System.Collections.Generic;
using Unity.Services.CloudSave;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public string AccountName;
    public string PlayerName;
    public int Score;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "AccountName", AccountName },
            { "PlayerName", PlayerName },
            { "Score", Score },
        };
    }

    public static PlayerData FromDictionary(Dictionary<string, Unity.Services.CloudSave.Models.Item> dict)
    {
        PlayerData data = new PlayerData();
        if (dict.TryGetValue("AccountName", out var acc)) data.AccountName = acc.Value.GetAs<string>();
        if (dict.TryGetValue("PlayerName", out var name)) data.PlayerName = name.Value.GetAs<string>();
        if (dict.TryGetValue("Score", out var score)) data.Score = score.Value.GetAs<int>();
        return data;
    }


}
