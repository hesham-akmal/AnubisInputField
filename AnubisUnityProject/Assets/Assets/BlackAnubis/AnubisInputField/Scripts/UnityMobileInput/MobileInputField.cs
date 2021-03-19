// ----------------------------------------------------------------------------
// The MIT License
// UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// Copyright (c) 2018-2020 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using NiceJson;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

#if UNITY_ANDROID

namespace Mopsicus.Plugins
{
    /// <summary>
    /// Wrapper for Unity InputField
    /// Add this component on your InputField
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class MobileInputField : MobileInputReceiver
    {
        /// <summary>
        /// Config structure
        /// </summary>
        private struct MobileInputConfig
        {
            public bool Multiline;
            public Color TextColor;
            public Color BackgroundColor;
            public string ContentType;
            public string InputType;
            public string KeyboardType;
            public float FontSize;
            public string Align;
            public string Placeholder;
            public Color PlaceholderColor;
            public int CharacterLimit;
        }

        /// <summary>
        /// Button type
        /// </summary>
        public enum ReturnKeyType
        {
            Default,
            Next,
            Done,
            Search,
            Send
        }

        /// <summary>
        /// "Done" button visible (for iOS)
        /// </summary>
        public bool IsWithDoneButton = true;

        /// <summary>
        /// "(x)" button visible (for iOS)
        /// </summary>
        public bool IsWithClearButton = true;

        /// <summary>
        /// Type for return button
        /// </summary>
        public ReturnKeyType ReturnKey;

        /// <summary>
        /// Action when Return pressed, for subscribe
        /// </summary>
        public Action OnReturnPressed = delegate { };

        /// <summary>
        /// Action when Focus changed
        /// </summary>
        //public Action<bool> OnFocusChanged = delegate { };

        /// <summary>
        /// Event when Return pressed, for Unity inspector
        /// </summary>
        //public UnityEvent OnReturnPressedEvent;

        /// <summary>
        /// Mobile input creation flag
        /// </summary>
        private bool _isMobileInputCreated = false;

        /// <summary>
        /// InputField object
        /// </summary>
        private AnubisInputField _inputField;

        /// <summary>
        /// Text object from _inputField
        /// </summary>
        private TMP_Text _inputObjectText;

        /// <summary>
        /// Current config
        /// </summary>
        private MobileInputConfig _config;

        /// <summary>
        /// InputField create event
        /// </summary>
        private const string CREATE = "CREATE_EDIT";

        /// <summary>
        /// InputField remove event
        /// </summary>
        private const string REMOVE = "REMOVE_EDIT";

        /// <summary>
        /// Set text to InputField
        /// </summary>
        private const string SET_TEXT = "SET_TEXT";

        /// <summary>
        /// Set focus to InputField
        /// </summary>
        private const string SET_FOCUS = "SET_FOCUS";

        /// <summary>
        /// Set visible to InputField
        /// </summary>
        private const string SET_VISIBLE = "SET_VISIBLE";

        /// <summary>
        /// Event when text changing in InputField
        /// </summary>
        private const string TEXT_CHANGE = "TEXT_CHANGE";

        /// <summary>
        /// Event when text end changing in InputField
        /// </summary>
        private const string TEXT_END_EDIT = "TEXT_END_EDIT";

        /// <summary>
        /// Event for Android
        /// </summary>
        private const string ANDROID_KEY_DOWN = "ANDROID_KEY_DOWN";

        /// <summary>
        /// Event when Return key pressed
        /// </summary>
        private const string RETURN_PRESSED = "RETURN_PRESSED";

        /// <summary>
        /// Event when caret pos changes
        /// </summary>
        private const string SYNC_CARET = "SYNC_CARET";

        /// <summary>
        /// Ready event
        /// </summary>
        private const string READY = "READY";

        private string lastReceivedNativeText;

        protected override void Start()
        {
            base.Start();
            CreateNative();
        }

        public void CreateNative()
        {
            _inputField = this.GetComponent<AnubisInputField>();
            lastReceivedNativeText = "";
            //Use this class only for android, for now
#if !UNITY_ANDROID

            Destroy(this);
            return;

#endif

            print("CreateNative");

            if ((object)_inputField == null)
            {
                Debug.LogError(string.Format("No found TMP_InputField for {0} MobileInput", this.name));
                throw new MissingComponentException();
            }
            _inputObjectText = _inputField.textComponent;

            StartCoroutine(InitialzieOnNextFrame());
        }

        /// <summary>
        /// Initialization coroutine
        /// </summary>
        private IEnumerator InitialzieOnNextFrame()
        {
            yield return null;
            this.PrepareNativeEdit();
#if UNITY_ANDROID && !UNITY_EDITOR
            this.CreateNativeEdit ();
#endif
        }

        /// <summary>
        /// Check position on each frame
        /// If changed - send to plugin
        /// It's need when app rotate on input field chage position
        /// </summary>
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            this.UpdateForceKeyeventForAndroid ();
#endif
            if (Input.GetMouseButtonDown(0) && _inputField.isFocused)
                SyncCaretPos();
        }

        public void SyncCaretPos()
        {
            JsonObject data = new JsonObject();
            data["msg"] = SYNC_CARET;
            data["caretPos"] = InvariantCultureString(_inputField.caretPosition);
            this.Execute(data);
        }

        /// <summary>
        /// Prepare config
        /// </summary>
        private void PrepareNativeEdit()
        {
            TMP_Text placeHolder = _inputField.placeholder.GetComponent<TMP_Text>();
            _config.Placeholder = placeHolder.text;

            //Set invisible as we're using unity text as visual only
            Color invisible = new Color(0, 0, 0, 0);
            _config.PlaceholderColor = invisible;
            _config.TextColor = invisible;
            _config.BackgroundColor = invisible;

            //Set with colors
            //_config.PlaceholderColor =   placeHolder.color;
            //_config.TextColor =  _inputObjectText.color;

            _config.CharacterLimit = _inputField.characterLimit;
            Rect rect = GetScreenRectFromRectTransform(this._inputObjectText.rectTransform);
            float ratio = rect.height / _inputObjectText.rectTransform.rect.height;
            _config.FontSize = ((float)_inputObjectText.fontSize) * ratio * 1.1f;
            _config.Align = "MiddleLeft"; //_inputObjectText.alignment.ToString();
            _config.ContentType = _inputField.contentType.ToString();
            _config.Multiline = _inputField.lineType != TMP_InputField.LineType.SingleLine;
            _config.KeyboardType = _inputField.keyboardType.ToString();
            _config.InputType = _inputField.inputType.ToString();
        }

        /// <summary>
        /// Text change callback
        /// </summary>
        /// <param name="text">new text</param>
        private void OnTextChange(string text, int caretPos)
        {
            lastReceivedNativeText = text;

            if (text.Equals(_inputField.text))
                return;

            //Sometimes on selecting non empty input field, this method receives empty text,
            //which shows field placeholder for 1 frame then returns to the original text. Wait 2 frames before setting empty.
            if (string.IsNullOrEmpty(text))
                StartCoroutine(nameof(TrySetEmptyText));
            else
            {
                StopCoroutine(nameof(TrySetEmptyText));
                this._inputField.text = text;
            }

            _inputField.caretPosition = caretPos;
            //_inputField.GetComponent<AnubisInputField>().ForceAdjustRectTransformDependingOnCaretPos();

            if (this._inputField.onValueChanged != null)
            {
                this._inputField.onValueChanged.Invoke(text);
            }
        }

        private IEnumerator TrySetEmptyText()
        {
            yield return null;
            yield return null;
            _inputField.text = "";
        }

        /// <summary>
        /// Text change end callback
        /// </summary>
        /// <param name="text">text</param>
        private void OnTextEditEnd(string text)
        {
            this._inputField.text = text;
            _inputField.onEndEdit?.Invoke(text);
            OnKeyboardHidden();
        }

        /// <summary>
        /// Sending data to plugin
        /// </summary>
        /// <param name="data">JSON</param>
        public override void Send(JsonObject data)
        {
            MobileInput.Plugin.StartCoroutine(PluginsMessageRoutine(data));
        }

        /// <summary>
        /// Remove focus, keyboard when app lose focus
        /// </summary>
        public override void Hide()
        {
            this.SetFocus(false);
        }

        /// <summary>
        /// Coroutine for send, so its not freeze main thread
        /// </summary>
        /// <param name="data">JSON</param>
        private IEnumerator PluginsMessageRoutine(JsonObject data)
        {
            yield return null;
            string msg = data["msg"];
            if (msg.Equals(TEXT_CHANGE))
            {
                string text = data["text"];
                int caretPos = data["caretPos"];
                this.OnTextChange(text, caretPos);
            }
            else if (msg.Equals(READY))
            {
                this.Ready();
            }
            else if (msg.Equals(TEXT_END_EDIT))
            {
                string text = data["text"];
                this.OnTextEditEnd(text);
            }
            else if (msg.Equals(RETURN_PRESSED))
            {
                OnReturnPressed();

                _inputField.onSubmit.Invoke(_inputField.text);
            }
        }

        public void OnKeyboardHidden()
        {
            SetFocus(false);
            GetComponent<AnubisInputField>().DeactivateInputField();
        }

        /// <summary>
        /// Get sizes and convert to current screen size
        /// </summary>
        /// <param name="rect">RectTranform from Gameobject</param>
        /// <returns>Rect</returns>
        public static Rect GetScreenRectFromRectTransform(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 screenCoord;
                if (rect.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenCoord = corners[i];
                }
                else
                {
                    screenCoord = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[i]);
                }
                if (screenCoord.x < xMin)
                {
                    xMin = screenCoord.x;
                }
                if (screenCoord.x > xMax)
                {
                    xMax = screenCoord.x;
                }
                if (screenCoord.y < yMin)
                {
                    yMin = screenCoord.y;
                }
                if (screenCoord.y > yMax)
                {
                    yMax = screenCoord.y;
                }
            }
            Rect result = new Rect(xMin, Screen.height - yMax, xMax - xMin, yMax - yMin);
            return result;
        }

        /// <summary>
        /// Convert float value to InvariantCulture string
        /// </summary>
        /// <param name="value">float value</param>
        /// <returns></returns>
        private string InvariantCultureString(float value)
        {
            return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Create native input field
        /// </summary>
        private void CreateNativeEdit()
        {
            Rect rect = GetScreenRectFromRectTransform(this._inputObjectText.rectTransform);
            JsonObject data = new JsonObject();
            data["msg"] = CREATE;
            data["x"] = InvariantCultureString(0); // InvariantCultureString(rect.x / Screen.width);
            data["y"] = InvariantCultureString(-1000); // InvariantCultureString((rect.y / Screen.height) - 10);
            data["width"] = InvariantCultureString(rect.width / Screen.width);
            data["height"] = InvariantCultureString(rect.height / Screen.height);
            data["character_limit"] = _config.CharacterLimit;
            data["text_color_r"] = InvariantCultureString(_config.TextColor.r);
            data["text_color_g"] = InvariantCultureString(_config.TextColor.g);
            data["text_color_b"] = InvariantCultureString(_config.TextColor.b);
            data["text_color_a"] = InvariantCultureString(_config.TextColor.a);
            data["back_color_r"] = InvariantCultureString(_config.BackgroundColor.r);
            data["back_color_g"] = InvariantCultureString(_config.BackgroundColor.g);
            data["back_color_b"] = InvariantCultureString(_config.BackgroundColor.b);
            data["back_color_a"] = InvariantCultureString(_config.BackgroundColor.a);
            data["font_size"] = InvariantCultureString(_config.FontSize);
            data["content_type"] = _config.ContentType;
            data["align"] = _config.Align;
            data["with_done_button"] = this.IsWithDoneButton;
            data["with_clear_button"] = this.IsWithClearButton;
            data["placeholder"] = _config.Placeholder;
            data["placeholder_color_r"] = InvariantCultureString(_config.PlaceholderColor.r);
            data["placeholder_color_g"] = InvariantCultureString(_config.PlaceholderColor.g);
            data["placeholder_color_b"] = InvariantCultureString(_config.PlaceholderColor.b);
            data["placeholder_color_a"] = InvariantCultureString(_config.PlaceholderColor.a);
            data["multiline"] = _config.Multiline;
            data["font"] = "default";
            data["input_type"] = _config.InputType;
            data["keyboard_type"] = _config.KeyboardType;
            switch (ReturnKey)
            {
                case ReturnKeyType.Next:
                    data["return_key_type"] = "Next";
                    break;

                case ReturnKeyType.Done:
                    data["return_key_type"] = "Done";
                    break;

                case ReturnKeyType.Search:
                    data["return_key_type"] = "Search";
                    break;

                default:
                    data["return_key_type"] = "Default";
                    break;
            }

            this.Execute(data);
        }

        /// <summary>
        /// New field successfully added
        /// </summary>
        private void Ready()
        {
            _isMobileInputCreated = true;

            if (!string.IsNullOrEmpty(_inputField.text))
                SetTextNative(_inputField.text);

            //print("Ready: Text sending to native: " + _inputField.text);

            SetVisible(true);
            SetFocus(true);
        }

        /// <summary>
        /// Set text to field
        /// </summary>
        /// <param name="text">New text</param>
        public void SetTextNative(string text)
        {
            if (lastReceivedNativeText.Equals(text))
                return;

            JsonObject data = new JsonObject();
            data["msg"] = SET_TEXT;
            data["caretPos"] = InvariantCultureString(_inputField.caretPosition);
            data["text"] = text;
            this.Execute(data);
        }

        protected override void OnDestroy()
        {
            RemoveNative();
            base.OnDestroy();
        }

        /// <summary>
        /// Remove field
        /// </summary>
        public void RemoveNative()
        {
            if (!_isMobileInputCreated) return;

            _isMobileInputCreated = false;
            JsonObject data = new JsonObject();
            data["msg"] = REMOVE;
            this.Execute(data);
        }

        /// <summary>
        /// Set focus on field
        /// </summary>
        /// <param name="isFocus">true | false</param>
        public void SetFocus(bool isFocus)
        {
            if (!_isMobileInputCreated)
                return;

            JsonObject data = new JsonObject();
            data["msg"] = SET_FOCUS;
            data["is_focus"] = isFocus;
            this.Execute(data);
        }

        /// <summary>
        /// Set field visible
        /// </summary>
        /// <param name="isVisible">true | false</param>
        public void SetVisible(bool isVisible)
        {
            if (!_isMobileInputCreated)
                return;

            JsonObject data = new JsonObject();
            data["msg"] = SET_VISIBLE;
            data["is_visible"] = isVisible;
            this.Execute(data);
        }

#if UNITY_ANDROID && !UNITY_EDITOR

        /// <summary>
        /// Send android button state
        /// </summary>
        /// <param name="key">Code</param>
        private void ForceSendKeydownAndroid (string key) {
            JsonObject data = new JsonObject ();
            data["msg"] = ANDROID_KEY_DOWN;
            data["key"] = key;
            this.Execute (data);
        }

        /// <summary>
        /// Keyboard handler
        /// </summary>
        private void UpdateForceKeyeventForAndroid () {
            if (UnityEngine.Input.anyKeyDown) {
                if (UnityEngine.Input.GetKeyDown (KeyCode.Backspace)) {
                    this.ForceSendKeydownAndroid ("backspace");
                }else {
                    foreach (char c in UnityEngine.Input.inputString) {
                        if (c == '\n') {
                            this.ForceSendKeydownAndroid ("enter");
                        } else {
                            this.ForceSendKeydownAndroid (Input.inputString);
                        }
                    }
                }
            }
        }
#endif
    }
}

#endif