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
#if UNITY_ANDROID
#pragma warning disable CS0618
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView {
    /// <summary>
    /// Pre-build script that does the following:
    /// - validates the project's Graphics API settings.
    /// - copies the files for a required Gecko extension
    ///   to Assets/Plugins/Android/assets/vuplex-webview-gecko-extension
    ///   so that it is included as an asset in the compiled APK.
    /// </summary>
    /// <remarks>
    /// You can omit the copied directory from version control by adding the following
    /// rule to your .gitignore file:
    ///
    /// ```
    /// # This gets automatically gets copied to this location by the AndroidGeckoBuildScript.
    /// Assets/Plugins/Android/assets/vuplex-webview-gecko-extension*
    /// ```
    /// </remarks>
    public class AndroidGeckoBuildScript : IPreprocessBuild {

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {

            if (buildTarget != BuildTarget.Android) {
                return;
            }
            _validateGraphicsApi();
            EditorUtils.ForceAndroidInternetPermission();
            EditorUtils.AssertThatOculusLowOverheadModeIsDisabled();
            EditorUtils.AssertThatSrpBatcherIsDisabled();
            _copyGeckoExtensionToPluginsFolder();
        }

        const string EXTENSION_DIRECTORY_NAME = "vuplex-webview-gecko-extension";

        /// <summary>
        /// Moves the Gecko extension assets folder from the Assets/Vuplex directory
        /// to the Assets/Plugins directory so that Unity detects it and automatically
        /// includes it in the APK.
        /// </summary>
        static void _copyGeckoExtensionToPluginsFolder() {
            try {
                var destinationExtensionPath = EditorUtils.PathCombine(new string[] { Application.dataPath, "Plugins", "Android", "assets", EXTENSION_DIRECTORY_NAME });
                var sourceExtensionPath = EditorUtils.FindDirectory(
                    EditorUtils.PathCombine(new string[] { Application.dataPath, "Vuplex", "WebView", "Plugins", "AndroidGecko", "assets", EXTENSION_DIRECTORY_NAME }),
                    null,
                    new string[] { destinationExtensionPath }
                );
                // Don't copy .meta files, since that can lead to warnings from Unity about duplicate GUIDs
                EditorUtils.CopyAndReplaceDirectory(sourceExtensionPath, destinationExtensionPath);
            } catch (Exception e) {
                // Fail the build.
                throw new BuildFailedException(e);
            }
        }

        static void _validateGraphicsApi() {

            #if !VUPLEX_DISABLE_GRAPHICS_API_WARNING
                var autoGraphicsApiEnabled = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
                if (autoGraphicsApiEnabled) {
                    throw new BuildFailedException("Graphics settings error: Vuplex 3D WebView for Android requires that \"Auto Graphics API\" be disabled in order to ensure that OpenGLES3 or OpenGLES2 is used. Please go to Player Settings, disable \"Auto Graphics API\", and set \"Graphics APIs\" to OpenGLES3 or OpenGLES2.");
                }
                var selectedGraphicsApi = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0];
                var error = Utils.GetGraphicsApiErrorMessage(selectedGraphicsApi, new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLES2 });
                if (error != null) {
                    throw new BuildFailedException(error);
                }
            #endif
        }
    }
}
#endif
