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
#pragma warning disable CS0108
#pragma warning disable CS0067
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView {

    /// <summary>
    /// The IWebView implementation used by 3D WebView for Android with Gecko Engine.
    /// This class also includes extra methods for Gecko-specific functionality.
    /// </summary>
    /// <remarks>
    /// The Android Gecko plugin supports dragging within a web page to select
    /// text but doesn't support drag-and-drop interactions.
    /// </remarks>
    public class AndroidGeckoWebView : BaseWebView,
                                       IWebView,
                                       IWithDownloads,
                                       IWithKeyDownAndUp,
                                       IWithMovablePointer,
                                       IWithPointerDownAndUp,
                                       IWithPopups {

        [Obsolete("AndroidGeckoWebView.DownloadRequested has been removed. Please use the new IWithDownloads interface instead: https://developer.vuplex.com/webview/IWithDownloads", true)]
        public event EventHandler DownloadRequested;

        /// <see cref="IWithDownloads"/>
        public event EventHandler<DownloadChangedEventArgs> DownloadProgressChanged;

        /// <summary>
        /// Indicates that the page requested a file selection dialog. This can happen, for example, when a file input
        /// is activated. Call `FileSelectionEventArgs.Continue(filePaths)` to provide a file selection or call
        /// `FileSelectionEventArgs.Cancel()` to cancel file selection.
        /// </summary>
        public event EventHandler<FileSelectionEventArgs> FileSelectionRequested {
            add {
                _assertSingletonEventHandlerUnset(_fileSelectionHandler, "FileSelectionRequested");
                _fileSelectionHandler = value;
                _webView.Call("setFileSelectionHandler", new AndroidGeckoFileSelectionCallback(_handleFileSelection));
            }
            remove {
                if (_fileSelectionHandler == value) {
                    _fileSelectionHandler = null;
                    _webView.Call("setFileSelectionHandler", null);
                }
            }
        }

        /// <see cref="IWithPopups"/>
        public event EventHandler<PopupRequestedEventArgs> PopupRequested;

        /// <summary>
        /// Event raised when a script in the page calls `window.alert()`.
        /// </summary>
        /// <remarks>
        /// If no handler is attached to this event, then `window.alert()` will return
        /// immediately and the script will continue execution. If a handler is attached to
        /// this event, then script execution will be paused until `ScriptDialogEventArgs.Continue()`
        /// is called.
        /// </remarks>
        public event EventHandler<ScriptDialogEventArgs> ScriptAlerted {
            add {
                _assertSingletonEventHandlerUnset(_scriptAlertHandler, "ScriptAlerted");
                _scriptAlertHandler = value;
                _webView.Call("setScriptAlertHandler", new AndroidGeckoStringAndBoolDelegateCallback(_handleScriptAlert));
            }
            remove {
                if (_scriptAlertHandler == value) {
                    _scriptAlertHandler = null;
                    _webView.Call("setScriptAlertHandler", null);
                }
            }
        }

        /// <summary>
        /// Event raised when a script in the page calls `window.confirm()`.
        /// </summary>
        /// <remarks>
        /// If no handler is attached to this event, then `window.confirm()` will return
        /// `false` immediately and the script will continue execution. If a handler is attached to
        /// this event, then script execution will be paused until `ScriptDialogEventArgs<bool>.Continue()`
        /// is called, and `window.confirm()` will return the value passed to `Continue()`.
        /// </remarks>
        public event EventHandler<ScriptDialogEventArgs<bool>> ScriptConfirmRequested {
            add {
                _assertSingletonEventHandlerUnset(_scriptConfirmHandler, "ScriptConfirmRequested");
                _scriptConfirmHandler = value;
                _webView.Call("setScriptConfirmHandler", new AndroidGeckoStringAndBoolDelegateCallback(_handleScriptConfirm));
            }
            remove {
                if (_scriptConfirmHandler == value) {
                    _scriptConfirmHandler = null;
                    _webView.Call("setScriptConfirmHandler", null);
                }
            }
        }

        public WebPluginType PluginType {
            get {
                return WebPluginType.AndroidGecko;
            }
        }

        public override Vector2 SizeInPixels {
            get {
                return new Vector2(
                    _webView.Call<int>("getScaledWidth"),
                    _webView.Call<int>("getScaledHeight")
                );
            }
        }

        public static AndroidGeckoWebView Instantiate() {

            return (AndroidGeckoWebView) new GameObject().AddComponent<AndroidGeckoWebView>();
        }

        public override void Init(Texture2D viewportTexture, float width, float height, Texture2D videoTexture) {

            _init(viewportTexture, width, height, videoTexture, null);
        }

        public override void Blur() {

            _assertValidState();
            _webView.Call("blur");
        }

        public override void CanGoBack(Action<bool> callback) {

            _assertValidState();
            var result = _webView.Call<bool>("canGoBack");
            callback(result);
        }

        public override void CanGoForward(Action<bool> callback) {

            _assertValidState();
            var result = _webView.Call<bool>("canGoForward");
            callback(result);
        }

        /// <summary>
        /// Overrides `BaseWebView.CaptureScreenshot()` because it doesn't work
        /// with Android OES textures.
        /// </summary>
        public override void CaptureScreenshot(Action<byte[]> callback) {

            _assertValidState();
            _webView.Call("captureScreenshot", new AndroidGeckoByteArrayCallback(callback));
        }

        public static void ClearAllData() {

            _class.CallStatic("clearAllData");
        }

        public override void Click(Vector2 point) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("click", nativeX, nativeY, false);
        }

        public override void Click(Vector2 point, bool preventStealingFocus) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("click", nativeX, nativeY, preventStealingFocus);
        }

        public override void Copy() {

            _assertValidState();
            KeyDown("c", KeyModifier.Control);
            KeyUp("c", KeyModifier.Control);
        }

        public override void Cut() {

            _assertValidState();
            KeyDown("x", KeyModifier.Control);
            KeyUp("x", KeyModifier.Control);
        }

        public override void DisableViewUpdates() {

            _assertValidState();
            _webView.Call("disableViewUpdates");
            _viewUpdatesAreEnabled = false;
        }

        public override void Dispose() {

            _assertValidState();
            // Cancel the render if it has been scheduled via GL.IssuePluginEvent().
            //
            // BEGIN ForeVR Games Mod (NOMO) Null check for webview.
            //
            if (_webView != null) {
                WebView_removePointer(_webView.GetRawObject());
                IsDisposed = true;
                _webView.Call("destroy");
                _webView.Dispose();
            }
            //
            // END ForeVR Games Mod
            //
            Destroy(gameObject);
        }

        /// <summary>
        /// Enables remote debugging with FireFox's dev tools.
        /// Note that this method can only be called prior to
        /// creating any webviews.
        /// </summary>
        /// <remarks>
        /// If remote debugging is enabled, you can connect your
        /// device to your dev computer and remotely debug webview
        /// instances by navigating to `about:debugging` in FireFox
        /// on the dev computer.
        /// For more information on remote debugging, please see
        /// [this support article](https://support.vuplex.com/articles/how-to-debug-web-content#androidgecko).
        /// </remarks>
        public static void EnableRemoteDebugging() {

            var success = _class.CallStatic<bool>("enableRemoteDebugging");
            if (!success) {
                throw new InvalidOperationException(REMOTE_DEBUGGING_EXCEPTION_MESSAGE);
            }
        }

        public override void EnableViewUpdates() {

            _assertValidState();
            _webView.Call("enableViewUpdates");
            _viewUpdatesAreEnabled = true;
        }

        /// <summary>
        /// Ensures that the built-in extension is installed using GeckoView's `WebExtensionController.ensureBuiltIn()` method.
        /// The extension is not re-installed if it's already present and it has the same version.
        /// </summary>
        /// <example>
        /// #if UNITY_ANDROID &gt;&gt; !UNITY_EDITOR
        ///     AndroidGeckoWebView.EnsureBuiltInExtension("resource://android/assets/example/", "example@example.com");
        /// #endif
        /// </example>
        /// <param name="uri">Folder where the extension is located. To ensure this folder is inside the APK, only resource://android URIs are allowed.</param>
        /// <param name="id">Extension ID as present in the manifest.json file.</param>
        public static void EnsureBuiltInExtension(string uri, string id) {

            _class.CallStatic("ensureBuiltInExtension", uri, id);
        }

        public override void ExecuteJavaScript(string javaScript, Action<string> callback) {

            _assertValidState();
            string resultCallbackId = null;
            if (callback != null) {
                resultCallbackId = Guid.NewGuid().ToString();
                _pendingJavaScriptResultCallbacks[resultCallbackId] = callback;
            }
            _webView.Call("executeJavaScript", javaScript, resultCallbackId);
        }

        public override void Focus() {

            _assertValidState();
            _webView.Call("focus");
        }

        /// <summary>
        /// Overrides `BaseWebView.GetRawTextureData()` because it's slow on Android.
        /// </summary>
        public override void GetRawTextureData(Action<byte[]> callback) {

            _assertValidState();
            _webView.Call("getRawTextureData", new AndroidGeckoByteArrayCallback(callback));
        }

        public static void GloballySetUserAgent(bool mobile) {

            _class.CallStatic("globallySetUserAgent", mobile);
        }

        public static void GloballySetUserAgent(string userAgent) {

            _class.CallStatic("globallySetUserAgent", userAgent);
        }

        public override void GoBack() {

            _assertValidState();
            _webView.Call("goBack");
        }

        public override void GoForward() {

            _assertValidState();
            _webView.Call("goForward");
        }

        public override void HandleKeyboardInput(string key) {

            _assertValidState();
            _webView.Call("handleKeyboardInput", key);
        }

        /// <see cref="IWithKeyDownAndUp"/>
        public void KeyDown(string key, KeyModifier modifiers) {

            _assertValidState();
            _webView.Call("keyDown", key, (int)modifiers);
        }

        /// <see cref="IWithKeyDownAndUp"/>
        public void KeyUp(string key, KeyModifier modifiers) {

            _assertValidState();
            _webView.Call("keyUp", key, (int)modifiers);
        }

        public override void LoadHtml(string html) {

            _assertValidState();
            _webView.Call("loadHtml", html);
        }

        public override void LoadUrl(string url) {

            _assertValidState();
            _webView.Call("loadUrl", _transformStreamingAssetsUrlIfNeeded(url));
        }

        public override void LoadUrl(string url, Dictionary<string, string> additionalHttpHeaders) {

            _assertValidState();
            if (additionalHttpHeaders == null) {
                LoadUrl(url);
            } else {
                var map = _convertDictionaryToJavaMap(additionalHttpHeaders);
                _webView.Call("loadUrl", url, map);
            }
        }

        /// <see cref="IWithMovablePointer"/>
        public void MovePointer(Vector2 point) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("movePointer", nativeX, nativeY);
        }

        public override void Paste() {

            _assertValidState();
            KeyDown("v", KeyModifier.Control);
            KeyUp("v", KeyModifier.Control);
        }

        /// <summary>
        /// Pauses processing, media, and rendering for this webview instance
        /// until `Resume()` is called.
        /// </summary>
        public void Pause() {

            _assertValidState();
            _webView.Call("pause");
        }

        /// <summary>
        /// Pauses processing, media, and rendering for all webview instances.
        /// This method is automatically called by the plugin when the application
        /// is paused.
        /// </summary>
        public static void PauseAll() {

            _class.CallStatic("pauseAll");
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point) {

            _pointerDown(point, MouseButton.Left);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _logClickCountWarningIfNeeded(options.ClickCount, "PointerDown");
            _pointerDown(point, options.Button);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point) {

            _pointerUp(point, MouseButton.Left);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _logClickCountWarningIfNeeded(options.ClickCount, "PointerUp");
            _pointerUp(point, options.Button);
        }

        public override void Reload() {

            _assertValidState();
            _webView.Call("reload");
        }

        /// <summary>
        /// Resumes processing and rendering for all webview instances
        /// after a previous call to `Pause().`
        /// </summary>
        public void Resume() {

            _assertValidState();
            _webView.Call("resume");
        }

        /// <summary>
        /// Resumes processing and rendering for all webview instances
        /// after a previous call to `PauseAll().` This method
        /// is automatically called by the plugin when the application resumes after
        /// being paused.
        /// </summary>
        public static void ResumeAll() {

            _class.CallStatic("resumeAll");
        }

        public override void Scroll(Vector2 scrollDelta) {

            _assertValidState();
            var deltaX = (int)(scrollDelta.x * _numberOfPixelsPerUnityUnit);
            var deltaY = (int)(scrollDelta.y * _numberOfPixelsPerUnityUnit);
            _webView.Call("scroll", deltaX, deltaY);
        }

        public override void Scroll(Vector2 scrollDelta, Vector2 point) {

            _assertValidState();
            var deltaX = (int)(scrollDelta.x * _numberOfPixelsPerUnityUnit);
            var deltaY = (int)(scrollDelta.y * _numberOfPixelsPerUnityUnit);
            var pointerX = (int)(point.x * _nativeWidth);
            var pointerY = (int)(point.y * _nativeHeight);
            _webView.Call("scroll", deltaX, deltaY, pointerX, pointerY);
        }

        public override void SelectAll() {

            _assertValidState();
            KeyDown("a", KeyModifier.Control);
            KeyUp("a", KeyModifier.Control);
        }

        /// <summary>
        /// By default, web pages cannot access the device's
        /// camera or microphone via JavaScript, even if the user has granted
        /// the app permission to use them. Invoking `SetAudioAndVideoCaptureEnabled(true)` allows
        /// **all web pages** to access the camera and microphone if the user has
        /// granted the app permission to use them via the standard Android permission dialogs.
        /// </summary>
        /// <remarks>
        /// This is useful, for example, to enable WebRTC support.
        /// In addition to calling this method, the application must include the following Android
        /// permissions in its AndroidManifest.xml and also request the permissions at runtime.
        /// - android.permission.RECORD_AUDIO
        /// - android.permission.MODIFY_AUDIO_SETTINGS
        /// - android.permission.CAMERA
        /// </remarks>
        public static void SetAudioAndVideoCaptureEnabled(bool enabled) {

            _class.CallStatic("setAudioAndVideoCaptureEnabled", enabled);
        }

        /// <summary>
        /// By default, the Gecko browser engine outputs debug messages to the
        /// Logcat logs, but you can use this method to disable that.
        /// </summary>
        public static void SetDebugLoggingEnabled(bool enabled) {

            _class.CallStatic("setDebugLoggingEnabled", enabled);
        }

        /// <see cref="IWithDownloads"/>
        public void SetDownloadsEnabled(bool enabled) {

            _assertValidState();
            _webView.Call("setDownloadsEnabled", enabled);
        }

        /// <summary>
        /// Enables WideVine DRM. Gecko disables DRM by default because it
        /// could potentially be used for tracking.
        /// </summary>
        /// <remarks>
        /// You can verify that DRM is enabled by using the DRM Stream Test
        /// on [this page](https://bitmovin.com/demos/drm).
        /// </remarks>
        public static void SetDrmEnabled(bool enabled) {

            _class.CallStatic("setDrmEnabled", enabled);
        }

        /// <summary>
        /// By default, web pages cannot access the device's
        /// geolocation via JavaScript, even if the user has granted
        /// the app permission to access location. Invoking `SetGeolocationPermissionEnabled(true)` allows
        /// **all web pages** to access the geolocation if the user has
        /// granted the app location permissions via the standard Android permission dialogs.
        /// </summary>
        /// <remarks>
        /// The following Android permissions must be included in the app's AndroidManifest.xml
        /// and also requested by the application at runtime:
        /// - android.permission.ACCESS_COARSE_LOCATION
        /// - android.permission.ACCESS_FINE_LOCATION
        ///
        /// Note that geolocation doesn't work on Oculus devices because they lack GPS support.
        /// </remarks>
        public static void SetGeolocationPermissionEnabled(bool enabled) {

            _class.CallStatic("setGeolocationPermissionEnabled", enabled);
        }

        public static void SetIgnoreCertificateErrors(bool ignore) {

            _class.CallStatic("setIgnoreCertificateErrors", ignore);
        }

        /// <see cref="IWithPopups"/>
        public void SetPopupMode(PopupMode popupMode) {

            _assertValidState();
            _webView.Call("setPopupMode", (int)popupMode);
        }

        public static void SetStorageEnabled(bool enabled) {

            _class.CallStatic("setStorageEnabled", enabled);
        }

        /// <summary>
        /// Sets the `android.view.Surface` to which the webview renders.
        /// This can be used, for example, to render to an Oculus
        /// [OVROverlay](https://developer.oculus.com/reference/unity/1.30/class_o_v_r_overlay).
        /// After this method is called, the webview no longer renders
        /// to its original texture and instead renders to the given surface.
        /// </summary>
        /// <example>
        /// var surface = ovrOverlay.externalSurfaceObject();
        /// // Set the resolution to 1 px / Unity unit
        /// // to make it easy to specify the size in pixels.
        /// webView.SetResolution(1);
        /// // Or if the webview is attached to a prefab, call WebViewPrefab.Resize()
        /// webView.WebView.Resize(surface.externalSurfaceWidth(), surface.externalSurfaceHeight());
        /// #if UNITY_ANDROID && !UNITY_EDITOR
        ///     (webView as AndroidGeckoWebView).SetSurface(surface);
        /// #endif
        /// </example>
        public void SetSurface(IntPtr surface) {

            _assertValidState();
            var surfaceObject = _convertIntPtrToAndroidJavaObject(surface);
            _webView.Call("setSurface", surfaceObject);
        }

        /// <summary>
        /// Sets the JavaScript for the Gecko engine's optional [user.js preferences file](https://developer.mozilla.org/en-US/docs/Mozilla/Preferences/A_brief_guide_to_Mozilla_preferences),
        /// which can be used to optionally modify the browser engine's settings.
        /// Note that this method can only be called prior to creating any webviews.
        /// </summary>
        /// <remarks>
        /// The engine's current settings can be viewed by loading the url "about:config" in a webview.
        /// The available preferences aren't well-documented, but the following pages list some of them:
        /// - [libpref's StaticPrefList.yaml](https://dxr.mozilla.org/mozilla-central/source/modules/libpref/init/StaticPrefList.yaml)
        /// - [libpref's all.js](https://dxr.mozilla.org/mozilla-central/source/modules/libpref/init/all.js)
        /// </remarks>
        /// <example>
        /// AndroidGeckoWebView.SetUserPreferences(@"
        ///     user_pref('security.fileuri.strict_origin_policy', false);
        ///     user_pref('formhelper.autozoom', false);
        /// ");
        /// </example>
        public static void SetUserPreferences(string preferencesJavaScript) {

            var success = _class.CallStatic<bool>("setUserPreferences", preferencesJavaScript);
            if (!success) {
                throw new InvalidOperationException(USER_PREFERENCES_EXCEPTION_MESSAGE);
            }
        }

        public override void ZoomIn() {

            _assertValidState();
            _webView.Call("zoomIn");
        }

        public override void ZoomOut() {

            _assertValidState();
            _webView.Call("zoomOut");
        }

        // Get a reference to AndroidJavaObject's hidden constructor that takes
        // the IntPtr for a jobject as a parameter.
        readonly static ConstructorInfo _androidJavaObjectIntPtrConstructor = typeof(AndroidJavaObject).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new []{ typeof(IntPtr) },
            null
        );
        internal static AndroidJavaClass _class = new AndroidJavaClass(FULL_CLASS_NAME);
        // Hides BaseWebView._dllName
        protected const string _dllName = "VuplexWebViewAndroidGecko";
        EventHandler<FileSelectionEventArgs> _fileSelectionHandler;
        const string FULL_CLASS_NAME = "com.vuplex.webview.gecko.GeckoWebView";
        const string REMOTE_DEBUGGING_EXCEPTION_MESSAGE = "Unable to enable remote debugging, because a webview has already been created. EnableRemoteDebugging() can only be called prior to creating any webviews.";
        EventHandler<ScriptDialogEventArgs> _scriptAlertHandler;
        EventHandler<ScriptDialogEventArgs<bool>> _scriptConfirmHandler;
        const string USER_PREFERENCES_EXCEPTION_MESSAGE = "Unable to set user preferences, because a webview has already been created. SetUserPreferences() can only be called prior to creating any webviews.";
        readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
        internal AndroidJavaObject _webView;

        void _assertSingletonEventHandlerUnset(object handler, string eventName) {

            if (handler != null) {
                throw new InvalidOperationException(eventName + " supports only one event handler. Please remove the existing handler before adding a new one.");
            }
        }

        AndroidJavaObject _convertDictionaryToJavaMap(Dictionary<string, string> dictionary) {

            AndroidJavaObject map = new AndroidJavaObject("java.util.HashMap");
            IntPtr putMethod = AndroidJNIHelper.GetMethodID(map.GetRawClass(), "put", "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
            foreach (var entry in dictionary) {
                AndroidJNI.CallObjectMethod(
                    map.GetRawObject(),
                    putMethod,
                    AndroidJNIHelper.CreateJNIArgArray(new object[] { entry.Key, entry.Value })
                );
            }
            return map;
        }

        static AndroidJavaObject _convertIntPtrToAndroidJavaObject(IntPtr jobject) {

            if (jobject == IntPtr.Zero) {
                return null;
            }
            return (AndroidJavaObject) _androidJavaObjectIntPtrConstructor.Invoke(new object[] { jobject });
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleDownloadProgressChanged(string serializedMessage) {

            var handler = DownloadProgressChanged;
            if (handler != null) {
                var message = DownloadMessage.FromJson(serializedMessage);
                handler(this, message.ToEventArgs());
            }
        }

        void _handleFileSelection(string serializedMessage, Action<string[]> continueCallback, Action cancelCallback) {

            var message = FileSelectionMessage.FromJson(serializedMessage);
            var eventArgs = new FileSelectionEventArgs(
                message.AcceptFilters,
                message.MultipleAllowed,
                continueCallback,
                cancelCallback
            );
            _fileSelectionHandler(this, eventArgs);
        }

        void _handlePopup(string url, AndroidJavaObject session) {

            var handler = PopupRequested;
            if (handler == null) {
                return;
            }
            if (session == null) {
                handler(this, new PopupRequestedEventArgs(url, null));
                return;
            }
            var popupWebView = Instantiate();
            Dispatcher.RunOnMainThread(() => {
                AndroidGeckoWebPlugin.Instance.CreateTexture(1, 1, texture => {
                    // Use the same resolution and dimensions as the current webview.
                    popupWebView.SetResolution(_numberOfPixelsPerUnityUnit);
                    popupWebView._init(texture, _width, _height, null, session);
                    handler(this, new PopupRequestedEventArgs(url, popupWebView));
                });
            });
        }

        void _handleScriptAlert(string message, Action<bool> continueCallback) {

            _scriptAlertHandler(this, new ScriptDialogEventArgs(message, () => continueCallback(true)));
        }

        void _handleScriptConfirm(string message, Action<bool> continueCallback) {

            _scriptConfirmHandler(this, new ScriptDialogEventArgs<bool>(message, continueCallback));
        }

        void _init(Texture2D viewportTexture, float width, float height, Texture2D videoTexture, AndroidJavaObject popupSession) {

            base.Init(viewportTexture, width, height, videoTexture);
            _webView = new AndroidJavaObject(
                FULL_CLASS_NAME,
                gameObject.name,
                viewportTexture.GetNativeTexturePtr().ToInt32(),
                _nativeWidth,
                _nativeHeight,
                SystemInfo.graphicsMultiThreaded,
                new AndroidGeckoStringAndObjectCallback(_handlePopup),
                popupSession
            );
        }

        void _logClickCountWarningIfNeeded(int clickCount, string methodName) {

            if (clickCount > 1) {
                WebViewLogger.LogWarningFormat("AndroidGeckoWebView.{0}() called with a ClickCount > 1 (e.g. to trigger a double click), but the Gecko browser engine only supports single clicks on Android.", methodName);
            }
        }

        void OnEnable() {

            // Start the coroutine from OnEnable so that the coroutine
            // is restarted if the object is deactivated and then reactivated.
            // The render event only needs to be explicitly dispatched to the
            // plugin when multithreaded rendering is enabled. When it's disabled,
            // the web rendering is decoupled from Unity rendering.
            if (SystemInfo.graphicsMultiThreaded) {
                StartCoroutine(_renderPluginOncePerFrame());
            }
        }

        void _pointerDown(Vector2 point, MouseButton mouseButton) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("pointerDown", nativeX, nativeY, (int)mouseButton);
        }

        void _pointerUp(Vector2 point, MouseButton mouseButton) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("pointerUp", nativeX, nativeY, (int)mouseButton);
        }

        IEnumerator _renderPluginOncePerFrame() {

            while (true) {
                yield return _waitForEndOfFrame;

                if (!_viewUpdatesAreEnabled || IsDisposed || _webView == null) {
                    continue;
                }
                var nativeWebViewPtr = _webView.GetRawObject();
                if (nativeWebViewPtr != IntPtr.Zero) {
                    int pointerId = WebView_depositPointer(nativeWebViewPtr);
                    GL.IssuePluginEvent(WebView_getRenderFunction(), pointerId);
                }
            }
        }

        protected override void _resize() {

            // Only trigger a resize if the webview has been initialized
            if (_viewportTexture) {
                _assertValidState();
                Utils.ThrowExceptionIfAbnormallyLarge(_nativeWidth, _nativeHeight);
                _webView.Call("resize", _nativeWidth, _nativeHeight);
            }
        }

        protected override void _setConsoleMessageEventsEnabled(bool enabled) {

            _assertValidState();
            _webView.Call("setConsoleMessageEventsEnabled", enabled);
        }

        protected override void _setFocusedInputFieldEventsEnabled(bool enabled) {

            _assertValidState();
            _webView.Call("setFocusedInputFieldEventsEnabled", enabled);
        }

        [DllImport(_dllName)]
        static extern IntPtr WebView_getRenderFunction();

        [DllImport(_dllName)]
        static extern int WebView_depositPointer(IntPtr pointer);

        [DllImport(_dllName)]
        static extern void WebView_removePointer(IntPtr pointer);
    }
}
#endif
