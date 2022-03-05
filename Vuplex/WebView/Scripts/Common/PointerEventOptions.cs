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
namespace Vuplex.WebView {

    /// <summary>
    /// Options to alter pointer events (i.e. mouse events).
    /// </summary>
    public class PointerEventOptions {

        /// <summary>
        /// The number of clicks for the event. For example, for a double click,
        /// set this value to `2`. The default is `1`.
        /// </summary>
        public int ClickCount = 1;

        /// <summary>
        /// The button for the event. The default is `MouseButton.Left`.
        /// </summary>
        public MouseButton Button = MouseButton.Left;
    }
}
