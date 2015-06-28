﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PLATFORM_IOS

using Foundation;
using UIKit;
using ObjCRuntime;

namespace SiliconStudio.Paradox.Games
{
    public class ParadoxGameController : UIViewController
    {
        public delegate void OnTouchesBegan(NSSet touchesSet, UIEvent evt);
        public delegate void OnTouchesMoved(NSSet touchesSet, UIEvent evt);
        public delegate void OnTouchesCancelled(NSSet touchesSet, UIEvent evt);
        public delegate void OnTouchesEnded(NSSet touchesSet, UIEvent evt);

        internal OnTouchesBegan TouchesBeganDelegate;
        internal OnTouchesMoved TouchesMovedDelegate;
        internal OnTouchesCancelled TouchesCancelledDelegate;
        internal OnTouchesEnded TouchesEndedDelegate;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // This lines of code are to set to fullscreen mode
            var sel = new Selector("setNeedsStatusBarAppearanceUpdate");

            if (RespondsToSelector(sel))
            {
                // iOS 7
                PerformSelector(sel, this, 0.0);
            }
            else
            {
                // iOS 6 and prior
                UIApplication.SharedApplication.SetStatusBarHidden(true, false);
            }
        }
        
        public override bool PrefersStatusBarHidden()
        {
            // iOS 7
            return true;
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);
            TouchesBeganDelegate(touches, evt);
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);
            TouchesMovedDelegate(touches, evt);
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);
            TouchesEndedDelegate(touches, evt);
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);
            TouchesCancelledDelegate(touches, evt);
        }
    }
}

#endif