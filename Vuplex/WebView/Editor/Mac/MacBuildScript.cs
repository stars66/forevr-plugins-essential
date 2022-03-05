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
#if UNITY_STANDALONE_OSX
#pragma warning disable CS0618
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine.Rendering;

namespace Vuplex.WebView {

    public class MacBuildScript : IPreprocessBuild {

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {

            if (buildTarget != BuildTarget.StandaloneOSX) {
                return;
            }
            #if !VUPLEX_DISABLE_GRAPHICS_API_WARNING
                var selectedGraphicsApi = PlayerSettings.GetGraphicsAPIs(buildTarget)[0];
                var error = Utils.GetGraphicsApiErrorMessage(selectedGraphicsApi, new GraphicsDeviceType[] { GraphicsDeviceType.Metal });
                if (error != null) {
                    throw new BuildFailedException(error);
                }
            #endif
        }

        [PostProcessBuild(700)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {

            if (target != BuildTarget.StandaloneOSX) {
                return;
            }
            _copyPluginIfNeeded(pathToBuiltProject);
            // Delete all of the .meta files added by Unity because they cause a codesign mismatch
            // which causes app notarization to fail.
            var metaFiles = Directory.GetFiles(pathToBuiltProject, "*.meta", SearchOption.AllDirectories);
            foreach (var file in metaFiles) {
                File.Delete(file);
            }
        }

        const string MAC_PLUGIN_NAME = "VuplexWebViewMac.bundle";

        /// <summary>
        /// Unity 2020.3 has a bug that prevents it from automatically copying 3D WebView's
        /// VuplexWebViewMac.bundle plugin into the built app, so MacBuildScript.cs currently
        /// copies VuplexWebViewMac.bundle into the app instead.
        /// </summary>
        static void _copyPluginIfNeeded(string pathToBuiltProject) {

            #if !VUPLEX_DISABLE_MAC_PLUGIN_COPYING
                // Use TryParse because older versions of Unity don't have the CreateXcodeProject option.
                var createXcodeProject = false;
                Boolean.TryParse(
                    EditorUserBuildSettings.GetPlatformSettings("OSXUniversal", "CreateXcodeProject"),
                    out createXcodeProject
                );
                if (createXcodeProject) {
                    throw new BuildFailedException("3D WebView build error: Unity 2020.3 has a bug that prevents it from automatically copying 3D WebView's VuplexWebViewMac.bundle plugin into the built app, so MacBuildScript.cs currently copies VuplexWebViewMac.bundle into the app instead. However, it doesn't support the 'Create Xcode Project' build option. If you want to leave the 'Create Xcode Project' option enabled, you can add the scripting symbol VUPLEX_DISABLE_MAC_PLUGIN_COPYING to your project to disable this behavior and update the metadata for VuplexWebViewMac.bundle to make it so that it's enabled for Standalone builds.");
                }
                var pluginSourcePath = EditorUtils.FindDirectory(EditorUtils.PathCombine(new string[] { Application.dataPath, "Vuplex", "WebView", "Plugins", "Mac", MAC_PLUGIN_NAME }));
                var pluginDestinationPath = EditorUtils.PathCombine(new string[] { pathToBuiltProject, "Contents", "Plugins", MAC_PLUGIN_NAME });
                EditorUtils.CopyAndReplaceDirectory(pluginSourcePath, pluginDestinationPath);
            #endif
        }
    }
}
#endif
