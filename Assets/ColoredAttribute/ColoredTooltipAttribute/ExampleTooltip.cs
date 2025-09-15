using UnityEngine;

public class ExampleTooltip : MonoBehaviour
{
    [ColoredTooltip("Number of damaged parts", TooltipColor.Red)]
    public int damagedPartsCount;

    [ColoredTooltip("Player Health Status", TooltipColor.Green)]
    public float playerHealth;

    [ColoredTooltip("Current Weapon Type", TooltipColor.Blue)]
    public string currentWeapon;

    [ColoredTooltip("Is Player Invincible", TooltipColor.Purple)]
    public bool isInvincible;
}
