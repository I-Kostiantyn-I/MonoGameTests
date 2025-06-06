// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
#if ANDROID
using Android.Content.PM;
#endif
#if IOS
using UIKit;
#endif


namespace Microsoft.Xna.Framework.Input.Touch
{
    /// <summary>
    /// Allows retrieval of capabilities information from touch panel device.
    /// </summary>
    public struct TouchPanelCapabilities
    {
        private bool hasPressure;
        private bool isConnected;
        private int maximumTouchCount;
        private bool initialized;

        internal void Initialize()
        {
            if (!initialized)
            {
                initialized = true;

                // There does not appear to be a way of finding out if a touch device supports pressure.
                // XNA does not expose a pressure value, so let's assume it doesn't support it.
                hasPressure = false;

#if WINDOWS
                maximumTouchCount = GetSystemMetrics(SM_MAXIMUMTOUCHES);
                isConnected = (maximumTouchCount > 0);
#elif ANDROID
                // http://developer.android.com/reference/android/content/pm/PackageManager.html#FEATURE_TOUCHSCREEN
                var pm = Game.Activity.PackageManager;
                isConnected = pm.HasSystemFeature(PackageManager.FeatureTouchscreen);
                if (pm.HasSystemFeature(PackageManager.FeatureTouchscreenMultitouchJazzhand))
                    maximumTouchCount = 5;
                else if (pm.HasSystemFeature(PackageManager.FeatureTouchscreenMultitouchDistinct))
                    maximumTouchCount = 2;
                else
                    maximumTouchCount = 1;
#elif IOS
                //iPhone supports 5, iPad 11
                isConnected = true;
                if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
                    maximumTouchCount = 5;
                else //Pad
                    maximumTouchCount = 11;
#else
                //Touch isn't implemented in OpenTK, so no linux or mac https://github.com/opentk/opentk/issues/80
                isConnected = false;
#endif
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if a touch device supports pressure.
        /// </summary>
        public bool HasPressure
        {
            get
            {
                return hasPressure;
            }
        }

        /// <summary>
        /// Returns true if a device is available for use.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
        }

        /// <summary>
        /// Returns the maximum number of touch locations tracked by the touch panel device.
        /// </summary>
        public int MaximumTouchCount
        {
            get
            {
                return maximumTouchCount;
            }
        }

#if WINDOWS
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, ExactSpelling = true)]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_MAXIMUMTOUCHES = 95;
#endif
    }
}
