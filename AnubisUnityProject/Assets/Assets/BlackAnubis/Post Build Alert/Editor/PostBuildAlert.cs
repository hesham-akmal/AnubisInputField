using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR

namespace BlackAnubis.Post_Build_Alert
{
    public class PostBuildAlert : MonoBehaviour, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private void CheckBuildFailed(string logString, string stackTrace, LogType type)
        {
            // FAILED TO BUILD
            if (type == LogType.Error)
                OnBuildFinished(false);
        }

        // CALLED WHEN BUILDING START
        public void OnPreprocessBuild(BuildReport report)
        {
            Application.logMessageReceived += CheckBuildFailed;
        }

        // CALLED AFTER SUCCESSFUL BUILD
        public void OnPostprocessBuild(BuildReport report)
        {
            OnBuildFinished(true);
        }

        private void OnBuildFinished(bool success)
        {
            Application.logMessageReceived -= CheckBuildFailed;
            PlayAlert(success);
        }

        public static async void PlayAlert(bool success)
        {
            string scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();

            string scriptDir = scriptPath.Replace(nameof(PostBuildAlert) + ".cs", "");

            string sfxPath = scriptDir + (success ? "Success.wav" : "Fail.wav");

            AudioClip currentClip = await LoadClip(sfxPath);

            PlayClip(currentClip);
        }

        private static async Task<AudioClip> LoadClip(string path)
        {
            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV))
            {
                uwr.SendWebRequest();

                // wrap tasks in try/catch, otherwise it'll fail silently
                try
                {
                    while (!uwr.isDone) await Task.Delay(5);

                    if (uwr.isNetworkError || uwr.isHttpError) Debug.Log($"{uwr.error}");
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Debug.Log($"{err.Message}, {err.StackTrace}");
                }
            }

            return clip;
        }

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtilClass.GetMethod(

#if UNITY_2020_2_OR_NEWER
                "PlayPreviewClip",
#else
                "PlayClip",
#endif

                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );
            method.Invoke(null, new object[] { clip, startSample, loop });
        }
    }
}

#endif