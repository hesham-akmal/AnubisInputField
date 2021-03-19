using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

#if UNITY_ANDROID

using Mopsicus.Plugins;

#endif

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editable text input field.
/// </summary>
///
public class AnubisInputField : TMP_InputField
{
    /// <summary>
    /// Hide keyboard only manually, if false then clicking outside keyboard bounds also hides keyboard
    /// </summary>
    public bool onlyManualKeyboardHide;

    public static GameObject FocusedAnubisInputField { get; private set; }

    public bool IsSelected { get; private set; }

    [SerializeField] private AnubisInputField handOffAnubisInputField;

#if UNITY_ANDROID && !UNITY_EDITOR

    private float _originalCaretBlinkRate;

    private MobileInputField _androidMobileInputField;

    private bool _showAndroidNativeKeyboard;

    public override void OnSelect(BaseEventData eventData)
    {
        ActivateInputField(eventData);
    }

    public void ActivateInputField(BaseEventData eventData = null)
    {
        //Don't focus instantly
        //To prevent some race conditions in the case of selecting a field while already another field is selected
        if (IsSelected)
            return;

        IsSelected = true;
        FocusedAnubisInputField = gameObject;
        StartCoroutine(OnSelectIE(eventData));
    }

    private IEnumerator OnSelectIE(BaseEventData eventData)
    {
        base.Select();
        yield return null;
        base.OnSelect(eventData);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Important
    protected override void Awake()
    {
        //Very important as this awake is called in the editor
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            return;
#endif

        base.Awake();

        //Prevent a TMP bug with caret staying visible after deselecting
        resetOnDeActivation = true;

        shouldHideMobileInput = true;

        //Needed as there is no selecting text for now
        onFocusSelectAll = false;

        //Because Awake gets called in the editor

        if (!Plugins.instance)
            new GameObject().AddComponent<Plugins>();

        //If inspector soft keyboard enabled for android
        _showAndroidNativeKeyboard = !shouldHideSoftKeyboard;
        //Setting this to true to hide original TouchScreenKeyboard for android
        shouldHideSoftKeyboard = true;

        selectionColor = new Color(0, 0, 0, 0);

        onValueChanged.AddListener(newText =>
        {
            if (_showAndroidNativeKeyboard && _androidMobileInputField != null)
            {
                if (string.IsNullOrEmpty(newText))
                    caretPosition = 0;

                _androidMobileInputField.SetTextNative(newText);
            }
        });

        onSelect.AddListener(delegate
            {
                print(gameObject.name + " onSelect");
                IsSelected = true;
                if (_showAndroidNativeKeyboard)
                    _androidMobileInputField = gameObject.AddComponent<MobileInputField>();
            });

        onEndEdit.AddListener(delegate
        {
            print(gameObject.name + " onEndEdit");
            if (_showAndroidNativeKeyboard && _androidMobileInputField != null)
                Destroy(_androidMobileInputField);
            OnDeselect();
        });

        onSubmit.AddListener(delegate
        {
            //print(gameObject.name + " onSubmit");
            if (_showAndroidNativeKeyboard && _androidMobileInputField != null)
                Destroy(_androidMobileInputField);
            OnDeselect();
            TryActivateAnubisInputField();
        });

        onValueChanged.AddListener(delegate
        {
            ForceAdjustRectTransformDependingOnCaretPos();
        });
    }

    protected override void Start()
    {
        base.Start();

        _originalCaretBlinkRate = caretBlinkRate;

        //Setting private field of "m_DoubleClickDelay" to 0, to disable double click for selection
        //IMPORTANT: CANNOT be set in awake
        typeof(TMP_InputField).GetField("m_DoubleClickDelay", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(GetComponent<TMP_InputField>(), 0);
    }

    //// Prevent caret from blinking while holding down touch ////////////////////////////
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        caretBlinkRate = 0;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        caretBlinkRate = _originalCaretBlinkRate;
    }

    //////////////////////////////////////////////////////////////////////////////////////////

    //// Hide keyboard when app loses focus, such as expanding notification bar
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (_showAndroidNativeKeyboard && _androidMobileInputField != null)
                Destroy(_androidMobileInputField);
            OnDeselect();
        }
    }

    //Base has no Update() to override
    public void Update()
    {
        CheckDeselectionUpdate();

        PreventTextSelection();

        void PreventTextSelection()
        {
            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            int stringPosition =
 m_CaretPosition < m_CaretSelectPosition ? textInfo.characterInfo[m_CaretPosition].index : textInfo.characterInfo[m_CaretSelectPosition].index;
            int length =
 m_CaretPosition < m_CaretSelectPosition ? stringSelectPositionInternal - stringPosition : stringPositionInternal - stringPosition;

            //Finger vertically moving while holding touch inside field
            if (length > 0)
            {
                m_CaretPosition = stringSelectPositionInternal;
                stringPositionInternal = stringSelectPositionInternal;
                _androidMobileInputField?.SyncCaretPos();
            }

            //TODO:Selection and sending selection to native keyboard instead of m_SoftKeyboard
            //m_SoftKeyboard.selection = new RangeInt(stringPosition, length);
        }
    }

    public void DeactivateInputField()
    {
        OnDeselect();
    }

    private void OnDeselect()
    {
        if (!IsSelected)
            return;
        IsSelected = false;

        print(gameObject.name + " OnDeselect ----------");

        //Only set focused to null if no other fields were selected
        if (FocusedAnubisInputField == gameObject)
        {
            FocusedAnubisInputField = null;
            EventSystem.current.SetSelectedGameObject(null);
        }

        base.OnDeselect(null);
    }

#endif

#if UNITY_IOS || UNITY_ANDROID

    //For android: Disable TMP_InputField automatic deselection due to unity's TouchKeyboard being null, as we're using another native android keyboard
    //So this will be called every frame for android
    //For ios: Disable event system OnDeselect as it has weird behaviour when selecting field while already selecting another
    public override void OnDeselect(BaseEventData eventData)
    {
    }

    private void CheckDeselectionUpdate()
    {
        if (IsSelected
            //For deselecting a field with OnlyManualKeyboardHide set as true. Prevent deselection on any click unless another field was selected
            && (!onlyManualKeyboardHide || (onlyManualKeyboardHide && FocusedAnubisInputField != gameObject))
            //On pointer down, check if its outside input field rect. Overriding EventSystem OnDeselect
            && Input.GetMouseButtonDown(0) && !RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), Input.mousePosition))
            DeactivateInputField();
    }

    /// <summary>
    /// When chars in the input field exceed the window size it automatically shifts right,
    /// Then when deleting the chars it doesn't shift left again in ios and android ( But works in the editor )
    /// This method will force adjust shift left when needed
    /// </summary>
    public void ForceAdjustRectTransformDependingOnCaretPos()
    {
        //In the base class (TMP_InputField), each frame checks the bool "m_isLastKeyBackspace",
        //when set to true then the method "AdjustRectTransformRelativeToViewport" will be called,
        //which allows input field rect to shift left when deleting text

        // Here we're setting "m_isLastKeyBackspace" in the base class to true, since it's a private field, we use reflection to access and set the value.
        typeof(TMP_InputField).GetField("m_isLastKeyBackspace", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(GetComponent<TMP_InputField>(), true);
    }

    private void TryActivateAnubisInputField()
    {
        if (handOffAnubisInputField != null)
            handOffAnubisInputField.ActivateInputField();
    }

#endif

#if UNITY_IOS && !UNITY_EDITOR

    //private bool justSelected;

    protected override void Awake()
    {
        base.Awake();

        shouldHideMobileInput = true;

        onValueChanged.AddListener(delegate
        {
            ForceAdjustRectTransformDependingOnCaretPos();
        });

        onEndEdit.AddListener(delegate
        {
            print("ios onEndEdit ");
            DeactivateInputField();
        });

        onSubmit.AddListener(delegate
        {
            print("ios onSubmit ");
            DeactivateInputField();
            TryActivateAnubisInputField();
        });
    }

    public override void OnSelect(BaseEventData eventData)
    {
        ActivateInputField(eventData);
    }

    public void ActivateInputField(BaseEventData eventData = null)
    {
        print("ios OnSelect " + gameObject.name);
        FocusedAnubisInputField = gameObject;
        IsSelected = true;
        StartCoroutine(OnSelectIE(eventData));
    }

    private IEnumerator OnSelectIE(BaseEventData eventData)
    {
        yield return null;
        FocusedAnubisInputField = gameObject;
        base.OnSelect(eventData);
    }

    public void DeactivateInputField(BaseEventData eventData = null)
    {
        if (!IsSelected) return;

        IsSelected = false;

        print("ios DeactivateInputField " + gameObject.name);

        StartCoroutine(DeactivateInputFieldIE(eventData));
    }

    private IEnumerator DeactivateInputFieldIE(BaseEventData eventData)
    {
        while (EventSystem.current.alreadySelecting)
            yield return null;

        EventSystem.current.SetSelectedGameObject(null);

        base.OnDeselect(eventData);
        FocusedAnubisInputField = null;
    }

    private void Update()
    {
        CheckDeselectionUpdate();
    }

#endif
}