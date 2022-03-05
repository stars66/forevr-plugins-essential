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
#if UNITY_STANDALONE_WIN
#pragma warning disable CS0618
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView {
    /// <summary>
    /// Windows build script that copies the Chromium plugin executable's files to the
    /// required location in the built application folder.
    /// </summary>
    public class WindowsBuildScript : IPreprocessBuild {

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {

            if (buildTarget != BuildTarget.StandaloneWindows) {
                return;
            }
            #if !VUPLEX_DISABLE_GRAPHICS_API_WARNING
                var selectedGraphicsApi = PlayerSettings.GetGraphicsAPIs(buildTarget)[0];
                var error = Utils.GetGraphicsApiErrorMessage(selectedGraphicsApi, new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 });
                if (error != null) {
                    throw new BuildFailedException(error);
                }
            #endif
        }

        [PostProcessBuild(700)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {

            if (!(target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)) {
                return;
            }
            var buildPluginDirectoryPath = _getBuiltPluginDirectoryPath(pathToBuiltProject);
            var sourceChromiumDirectory = EditorUtils.FindDirectory(EditorUtils.PathCombine(new string[] { Application.dataPath, "Vuplex", "WebView", "Plugins", "Windows", CHROMIUM_DIRECTORY_NAME }));
            var destinationChromiumDirectory = Path.Combine(buildPluginDirectoryPath, CHROMIUM_DIRECTORY_NAME);
            EditorUtils.CopyAndReplaceDirectory(sourceChromiumDirectory, destinationChromiumDirectory);
        }

        const string DLL_FILE_NAME = "VuplexWebViewWindows.dll";
        const string CHROMIUM_DIRECTORY_NAME = "VuplexWebViewChromium";

        static string _getBuiltPluginDirectoryPath(string pathToBuiltProject) {

            var productName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            var buildDirectoryPath = _getParentDirectoryOfFile(pathToBuiltProject, '/');
            var expectedPluginDirectoryPath = EditorUtils.PathCombine(new string[] { buildDirectoryPath, productName + "_Data", "Plugins" });
            var expectedVuplexPluginFilePath = Path.Combine(expectedPluginDirectoryPath, DLL_FILE_NAME);
            var pluginFilePath = EditorUtils.FindFile(expectedVuplexPluginFilePath, buildDirectoryPath);
            return _getParentDirectoryOfFile(pluginFilePath, Path.DirectorySeparatorChar);
        }

        static string _getParentDirectoryOfFile(string filePath, char pathSeparator) {

            var pathComponents = filePath.Split(new char[] { pathSeparator }).ToList();
            return String.Join(Path.DirectorySeparatorChar.ToString(), pathComponents.GetRange(0, pathComponents.Count - 1).ToArray());
        }
    }
}
#endif // UNITY_STANDALONE_WIN
