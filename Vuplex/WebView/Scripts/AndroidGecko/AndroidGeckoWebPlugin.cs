/**
* Copyright (c) 2021 Vuplex Inc. All rights reserved.
*
* Licensed under the Vuplex Commercial Software Library License, you may
* not use this file except in compliance with the License. You may obtain
* a copy of the License at
*
*     https://vuplex.com/commercial-library-license
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;

namespace Vuplex.WebView {

    class AndroidGeckoWebPlugin : MonoBehaviour, IWebPlugin {

        public static AndroidGeckoWebPlugin Instance {
            get {
                if (_instance == null) {
                    _instance = (AndroidGeckoWebPlugin) new GameObject("AndroidGeckoWebPlugin").AddComponent<AndroidGeckoWebPlugin>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        public WebPluginType Type {
            get {
                return WebPluginType.AndroidGecko;
            }
        }

        public void ClearAllData() {

            AndroidGeckoWebView.ClearAllData();
        }

        public void CreateTexture(float width, float height, Action<Texture2D> callback) {

            AndroidGeckoTextureCreator.Instance.CreateTexture(width, height, callback);
        }

        public void CreateMaterial(Action<Material> callback) {

            CreateTexture(1, 1, texture => {
                var materialName = XrUtils.SinglePassRenderingIsEnabled ? "AndroidSinglePassViewportMaterial"
                                                                        : "AndroidViewportMaterial";
                // Construct a new material, because Resources.Load<T>() returns a singleton.
                var material = new Material(Resources.Load<Material>(materialName));
                material.mainTexture = texture;
                callback(material);
            });
        }

        public void CreateVideoMaterial(Action<Material> callback) {

            callback(null);
        }

        public virtual IWebView CreateWebView() {

            return AndroidGeckoWebView.Instantiate();
        }

        public void EnableRemoteDebugging() {

            WebViewLogger.Log("Enabling remote debugging for Android Gecko. For instructions, please see https://support.vuplex.com/articles/how-to-debug-web-content#androidgecko.");
            AndroidGeckoWebView.EnableRemoteDebugging();
        }

        public void SetIgnoreCertificateErrors(bool ignore) {

            AndroidGeckoWebView.SetIgnoreCertificateErrors(ignore);
        }

        public void SetStorageEnabled(bool enabled) {

            AndroidGeckoWebView.SetStorageEnabled(enabled);
        }

        public void SetUserAgent(bool mobile) {

            AndroidGeckoWebView.GloballySetUserAgent(mobile);
        }

        public void SetUserAgent(string userAgent) {

            AndroidGeckoWebView.GloballySetUserAgent(userAgent);
        }

        static AndroidGeckoWebPlugin _instance;

        /// <summary>
        /// Automatically pause web processing and media playback
        /// when the app is paused and resume it when the app is resumed.
        /// </summary>
        void OnApplicationPause(bool isPaused) {

            if (isPaused) {
                AndroidGeckoWebView.PauseAll();
            } else {
                //
                // BEGIN ForeVR Games Mod (John) Moving ResumeAll to a coroutine so it doesn't happen instantly. That could cause the game rendering to fail to resume.
                //
                StartCoroutine (WaitBeforeResumeAll(0.1f));
                //
                // END ForeVR Games Mod
                //
            }
        }
        //
        // BEGIN ForeVR Games Mod (John) Moving ResumeAll to a coroutine so it doesn't happen instantly. That could cause the game rendering to fail to resume.
        //
        private IEnumerator WaitBeforeResumeAll(float timeToWait) {
            yield return new WaitForSeconds(timeToWait);
            AndroidGeckoWebView.ResumeAll();
        }
        //
        // END ForeVR Games Mod
        //
    }
}
#endif
