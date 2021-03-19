#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnubisInputField))]
public class AnubisInputFieldTMPEditor : TMP_InputFieldEditor
{
    /// <summary>
    /// Serialized target object
    /// </summary>
    private SerializedObject _object;

    private SerializedProperty _onlyManualKeyboardHide;

    private SerializedProperty _handOffAnubisInputField;

    protected override void OnEnable()
    {
        base.OnEnable();
        _object = new SerializedObject(target);
        _onlyManualKeyboardHide = _object.FindProperty("onlyManualKeyboardHide");
        _handOffAnubisInputField = serializedObject.FindProperty("handOffAnubisInputField");
    }

    public override void OnInspectorGUI()
    {
        _onlyManualKeyboardHide.serializedObject.Update();
        _handOffAnubisInputField.serializedObject.Update();

        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ANUBIS INPUT FIELD", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("IMPORTANT:\nChanging \"Hide Mobile Input\" OR \"Reset On DeActivation\" has no effect for android and ios (Will always be true).\nChanging \"On Focus - Select All\" has no effect for android (Will always be false).", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_onlyManualKeyboardHide, new GUIContent("Only Manual Keyboard Hide", "Hide keyboard only manually, if false then clicking outside keyboard bounds also hides keyboard."));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_handOffAnubisInputField, new GUIContent("Hand Off AnubisInputField", "On keyboard submit will pass the focus to attached field here."));

        EditorGUILayout.Space();

        _onlyManualKeyboardHide.serializedObject.ApplyModifiedProperties();
        _handOffAnubisInputField.serializedObject.ApplyModifiedProperties();
    }
}

#endif