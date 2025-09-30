using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// === Database ===
[CreateAssetMenu(menuName = "Game/UnitDatabase")]
public class UnitDatabase : ScriptableObject
{
    public List<UnitData> units;

    public UnitData GetById(string id)
    {
        return units.Find(u => u.id == id);
    }
}
