#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InjectAnubisInputField : MonoBehaviour
{
    [MenuItem("GameObject/Anubis/Replace any TMP_InputField to AnubisInputFieldTMP")]
    public static void Convert()
    {
        print("Start replacing any TMP_InputField to AnubisInputFieldTMP");

        int i = 0;

        //For each TMP_Input field in the loaded scene
        foreach (var tmp_InputField in FindAllObjectsOfTypeExpensive<TMPro.TMP_InputField>().ToList())
        {
            var inputFieldGo = tmp_InputField.gameObject;

            //Creating a tmp gameobj to hold the same serialized properties of current TMP_InputField script,
            //as we cannot AddComponent of an AnubisInputField script while TMP_InputField script still attached to gameobj
            var tmpGoTMP_InputField = new GameObject().AddComponent<TMP_InputField>();

            //Copy and paste serialized properties of current TMP_InputField script to temp gameobj TMP_InputField script
            EditorUtility.CopySerialized(tmp_InputField, tmpGoTMP_InputField);

            //Remove TMP_InputField script, to replace with AnubisInputField
            DestroyImmediate(tmp_InputField);

            //Create AnubisInputField and paste TMP_InputField serialized properties
            ComponentUtil.CopyPastSerialized(tmpGoTMP_InputField, inputFieldGo.AddComponent<AnubisInputField>());

            //Remove tempGo as its not needed anymore
            DestroyImmediate(tmpGoTMP_InputField.gameObject);

            print("Replaced: " + inputFieldGo.name);
            i++;
        }

        print("Successfully Replaced " + i + " TMP Input Fields");
    }

    [MenuItem("GameObject/Anubis/Replace any AnubisInputFieldTMP to TMP_InputFields")]
    public static void Revert()
    {
        print("Start replacing any AnubisInputFieldTMP to TMP_InputFields");

        int i = 0;

        foreach (var aif_InputField in FindAllObjectsOfTypeExpensive<AnubisInputField>().ToList())
        {
            var inputFieldGo = aif_InputField.gameObject;

            var tempInputField = new GameObject().AddComponent<AnubisInputField>();

            EditorUtility.CopySerialized(aif_InputField, tempInputField);

            DestroyImmediate(aif_InputField);

            ComponentUtil.CopyPastSerialized(tempInputField, inputFieldGo.AddComponent<TMP_InputField>());

            DestroyImmediate(tempInputField.gameObject);

            print("Replaced: " + inputFieldGo.name);
            i++;
        }

        print("Successfully Replaced " + i + " Anubis Input Fields");
    }

    public static IEnumerable<GameObject> GetAllRootGameObjects()
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var rootObjs = SceneManager.GetSceneAt(i).GetRootGameObjects();
            foreach (var obj in rootObjs)
                yield return obj;
        }
    }

    public static IEnumerable<T> FindAllObjectsOfTypeExpensive<T>() where T : MonoBehaviour
    {
        return from obj in GetAllRootGameObjects()
               from child in obj.GetComponentsInChildren<T>(true)
               where child.GetType() == typeof(T)
               select child;
    }
}

/// <summary>
/// By stektpotet
/// https://www.reddit.com/r/Unity3D/comments/94rpgc/i_wrote_a_piece_of_code_that_allows_for/
/// </summary>
public static class ComponentUtil
{
    public static void CopyPastSerialized(Object componentSource, Object componentTarget)
    {
        var source = new SerializedObject(componentSource);

        //check if they're the same type - if so do the ordinary copy/paste
        if (source.targetObject.GetType() == componentTarget.GetType())
        {
            EditorUtility.CopySerialized(source.targetObject, componentTarget);
            return;
        }
        SerializedObject dest = new SerializedObject(componentTarget);
        SerializedProperty prop_iterator = source.GetIterator();
        //jump into serialized object, this will skip script type so that we dont override the destination component's type
        if (prop_iterator.NextVisible(true))
        {
            while (prop_iterator.NextVisible(true)) //itterate through all serializedProperties
            {
                //try obtaining the property in destination component
                SerializedProperty prop_element = dest.FindProperty(prop_iterator.name);

                //validate that the properties are present in both components, and that they're the same type
                if (prop_element != null && prop_element.propertyType == prop_iterator.propertyType)
                {
                    //copy value from source to destination component
                    dest.CopyFromSerializedProperty(prop_iterator);
                }
            }
        }
        dest.ApplyModifiedProperties();
    }
}

#endif