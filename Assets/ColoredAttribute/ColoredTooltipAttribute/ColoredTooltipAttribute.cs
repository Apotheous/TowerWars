using System;
using UnityEditor;
using UnityEngine;

public class ColoredTooltipAttribute : PropertyAttribute
{
    public string Tooltip { get; private set; }
    public TooltipColor Color { get; private set; }

    public ColoredTooltipAttribute(string tooltip, TooltipColor color = TooltipColor.White)
    {
        Tooltip = tooltip;
        Color = color;
    }

}

public enum TooltipColor
{
    White,
    Red,
    Green,
    Blue,
    Yellow,
    Orange,
    Purple,
    Cyan,
    Pink
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ColoredTooltipAttribute))]
public class ColoredTooltipDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var tooltipAttribute = attribute as ColoredTooltipAttribute;
        Color tooltipColor = GetColorForTooltip(tooltipAttribute.Color);

        // Renkli tooltip metni oluþtur
        string colorHex = ColorUtility.ToHtmlStringRGB(tooltipColor);
        string coloredTooltip = $"<color=#{colorHex}>{tooltipAttribute.Tooltip}</color>";

        // Tooltip içeriðini ayarla
        var tooltipContent = new GUIContent(label.text, coloredTooltip);

        // Mouse tooltip alaný üzerindeyse arka plan çiz
        if (position.Contains(Event.current.mousePosition))
        {
            EditorGUI.DrawRect(
                new Rect(position.x - 2, position.y - 2, position.width + 4, EditorGUIUtility.singleLineHeight + 4),
                new Color(0.2f, 0.2f, 0.2f, 0.9f)
            );
        }

        // Property'i çiz
        EditorGUI.PropertyField(position, property, tooltipContent);
    }

    private Color GetColorForTooltip(TooltipColor color)
    {
        switch (color)
        {
            case TooltipColor.Red:
                return new Color(1f, 0.3f, 0.3f);
            case TooltipColor.Green:
                return new Color(0.3f, 0.85f, 0.3f);
            case TooltipColor.Blue:
                return new Color(0.3f, 0.5f, 1f);
            case TooltipColor.Yellow:
                return new Color(1f, 0.92f, 0.016f);
            case TooltipColor.Orange:
                return new Color(1f, 0.5f, 0.2f);
            case TooltipColor.Purple:
                return new Color(0.8f, 0.3f, 0.9f);
            case TooltipColor.Cyan:
                return new Color(0.3f, 0.8f, 0.9f);
            case TooltipColor.Pink:
                return new Color(1f, 0.4f, 0.7f);
            default:
                return Color.white;
        }
    }
}
#endif