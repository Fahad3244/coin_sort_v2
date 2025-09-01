// Assets/Editor/ConditionalFieldDrawer.cs
using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute conditional = (ConditionalFieldAttribute)attribute;
        SerializedProperty sourceProperty = GetSourceProperty(property, conditional.fieldName);

        bool show = Evaluate(sourceProperty, conditional);

        if (!show)
            return;

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute conditional = (ConditionalFieldAttribute)attribute;
        SerializedProperty sourceProperty = GetSourceProperty(property, conditional.fieldName);

        bool show = Evaluate(sourceProperty, conditional);
        return show ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
    }

    private SerializedProperty GetSourceProperty(SerializedProperty property, string fieldName)
    {
        // 1) Try global find (root-level)
        var so = property.serializedObject;
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null) return prop;

        // 2) Try sibling (most common for sibling fields inside same class)
        try
        {
            prop = property.FindPropertyRelative(fieldName);
            if (prop != null) return prop;
        }
        catch { /* ignore */ }

        // 3) Try building a full path (works for nested arrays/lists)
        string path = property.propertyPath; // e.g. "list.Array.data[0].coins.Array.data[1].unlockDirections"
        int lastDot = path.LastIndexOf('.');
        if (lastDot != -1)
        {
            string basePath = path.Substring(0, lastDot + 1);
            string newPath = basePath + fieldName;
            prop = so.FindProperty(newPath);
        }
        return prop;
    }

    private bool Evaluate(SerializedProperty sourceProperty, ConditionalFieldAttribute conditional)
    {
        if (sourceProperty == null)
        {
            // WARNING: Shows in Console so you know why the field is hidden
            Debug.LogWarning($"[ConditionalField] Could not find '{conditional.fieldName}' for property. Check the field name and that both fields are in the same serialized object.");
            return true; // safer: don't hide if we can't evaluate (change to false if you want strict hiding)
        }

        // Enums: compare by name (avoids index/value mismatch)
        if (sourceProperty.propertyType == SerializedPropertyType.Enum)
        {
            string currentName = sourceProperty.enumNames[sourceProperty.enumValueIndex];
            string expectedName = conditional.value.ToString();
            return currentName == expectedName;
        }

        // Bool
        if (sourceProperty.propertyType == SerializedPropertyType.Boolean)
        {
            bool expectedBool = Convert.ToBoolean(conditional.value);
            return sourceProperty.boolValue == expectedBool;
        }

        // Integer (fallback)
        if (sourceProperty.propertyType == SerializedPropertyType.Integer)
        {
            int expectedInt = Convert.ToInt32(conditional.value);
            return sourceProperty.intValue == expectedInt;
        }

        // Final fallback: compare string value
        return sourceProperty.stringValue == conditional.value?.ToString();
    }
}
