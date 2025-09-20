using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// === Database ===
[CreateAssetMenu(menuName = "Game/TurretDatabase")]
public class TurretDatabase : ScriptableObject
{
    public List<TurretData> turrets;

    public TurretData GetById(string id)
    {
        return turrets.Find(u => u.id == id);
    }
}
