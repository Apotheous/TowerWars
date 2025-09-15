using System;
using UnityEditor;
using UnityEngine;


// Özel Attribute
public class ColoredHeaderAttribute : PropertyAttribute
{
    public string Header { get; private set; }
    public HeaderColor Color { get; private set; }

    public ColoredHeaderAttribute(string header, HeaderColor color = HeaderColor.Blue)
    {
        Header = header;
        Color = color;
    }

}
public enum HeaderColor
{
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

[CustomPropertyDrawer(typeof(ColoredHeaderAttribute))]
public class ColoredHeaderDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var headerAttribute = attribute as ColoredHeaderAttribute;

        // Header için rect
        var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // Property için rect
        var propertyRect = new Rect(
            position.x,
            position.y + EditorGUIUtility.singleLineHeight + 2,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        // Renk seçimi
        Color headerColor = GetColorForHeader(headerAttribute.Color);

        // Header style
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = headerColor;
        style.fontSize = 12;

        // Header çizimi
        EditorGUI.LabelField(headerRect, headerAttribute.Header, style);

        // Property çizimi - orijinal label ile
        EditorGUI.PropertyField(propertyRect, property, new GUIContent(property.displayName), true);
    }

    private Color GetColorForHeader(HeaderColor color)
    {
        switch (color)
        {
            case HeaderColor.Red:
                return new Color(1f, 0.3f, 0.3f);
            case HeaderColor.Green:
                return new Color(0.3f, 0.85f, 0.3f);
            case HeaderColor.Blue:
                return new Color(0.3f, 0.5f, 1f);
            case HeaderColor.Yellow:
                return new Color(1f, 0.92f, 0.016f);
            case HeaderColor.Orange:
                return new Color(1f, 0.5f, 0.2f);
            case HeaderColor.Purple:
                return new Color(0.8f, 0.3f, 0.9f);
            case HeaderColor.Cyan:
                return new Color(0.3f, 0.8f, 0.9f);
            case HeaderColor.Pink:
                return new Color(1f, 0.4f, 0.7f);
            default:
                return Color.white;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + 4;
    }
}
#endif