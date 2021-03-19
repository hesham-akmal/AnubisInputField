// ----------------------------------------------------------------------------
// The MIT License
// UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// Copyright (c) 2018 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using NiceJson;

#if UNITY_ANDROID

namespace Mopsicus.Plugins
{
    /// <summary>
    /// Mobile plugin interface
    /// Each plugin must implement it
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Plaugin name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Callback on get data
        /// </summary>
        void OnData(JsonObject data);

        /// <summary>
        /// Callback on get error
        /// </summary>
        void OnError(JsonObject data);
    }

    /// <summary>
    /// Plugin service to manager all mobile plugins
    /// </summary>
    public class Plugins : MonoBehaviour
    {
        public static Plugins instance;

#if UNITY_ANDROID

        /// <summary>
        /// Mask for Java classes
        /// </summary>
        public const string ANDROID_CLASS_MASK = "ru.mopsicus.{0}.Plugin";

#endif

        /// <summary>
        /// Gameobject name on scene to receive data
        /// ACHTUNG! Do not change it
        /// </summary>
        private const string _dataObject = "Plugins";

        /// <summary>
        /// Function name to receive data
        /// ACHTUNG! Do not change it
        /// </summary>
        private const string _dataReceiver = "OnDataReceive";

        /// <summary>
        /// Dictionary of plugins
        /// </summary>
        private Dictionary<string, IPlugin> _plugins;

        private void Awake()
        {
            instance = this;
            name = _dataObject;
            DontDestroyOnLoad(gameObject);
            gameObject.name = "Plugins";
            InitPlugins();
        }

        private void OnDestroy()
        {
            _plugins = null;
        }

        /// <summary>
        /// Init all plugins in app
        /// </summary>
        private void InitPlugins()
        {
            gameObject.AddComponent<MobileInput>();
            //
            // other plugins
            //
            IPlugin[] plugins = GetComponents<IPlugin>();
            _plugins = new Dictionary<string, IPlugin>(plugins.Length);
            foreach (var item in plugins)
            {
                _plugins.Add(item.Name, item);
            }
            JsonObject data = new JsonObject();
            data["object"] = _dataObject;
            data["receiver"] = _dataReceiver;
            Debug.Log("Plugins init");
        }

        /// <summary>
        /// Handler to process data to plugin
        /// </summary>
        /// <param name="data">data from plugin</param>
        private void OnDataReceive(string data)
        {
            Debug.Log("Plugins receive data: " + data);
            try
            {
                JsonObject info = (JsonObject)JsonNode.ParseJsonString(data);
                if (_plugins.ContainsKey(info["name"]))
                {
                    IPlugin plugin = _plugins[info["name"]];
                    if (info.ContainsKey("error"))
                    {
                        plugin.OnError(info);
                    }
                    else
                    {
                        plugin.OnData(info);
                    }
                }
                else
                {
                    Debug.LogError(string.Format("{0} plugin does not exists", info["name"]));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Plugins receive error: {0}, stack: {1}", e.Message, e.StackTrace));
            }
        }
    }
}

#endif