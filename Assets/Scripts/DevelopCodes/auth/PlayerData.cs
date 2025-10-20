using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public string AccountName;
    public string PlayerName;
    //public int Level;
    public int Score;
    //public int Coins;
    //public string LastLogin;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "AccountName", AccountName },
            { "PlayerName", PlayerName },
            //{ "Level", Level },
            { "Score", Score },
            //{ "Coins", Coins },
            //{ "LastLogin", LastLogin }
        };
    }

    public static PlayerData FromDictionary(Dictionary<string, Unity.Services.CloudSave.Models.Item> dict)
    {
        PlayerData data = new PlayerData();
        if (dict.TryGetValue("PlayerName", out var name)) data.AccountName = name.Value.GetAs<string>();
        //if (dict.TryGetValue("Level", out var level)) data.Level = level.Value.GetAs<int>();
        if (dict.TryGetValue("Score", out var score)) data.Score = score.Value.GetAs<int>();
        //if (dict.TryGetValue("Coins", out var coins)) data.Coins = coins.Value.GetAs<int>();
        //if (dict.TryGetValue("LastLogin", out var login)) data.LastLogin = login.Value.GetAs<string>();
        return data;
    }
}
