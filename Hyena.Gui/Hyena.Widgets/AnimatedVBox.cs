//
// AnimatedVBox.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gdk;
using Gtk;

namespace Hyena.Widgets
{
    public class AnimatedVBox : AnimatedBox
    {
        public AnimatedVBox () : base (false)
        {
        }

        protected AnimatedVBox (IntPtr raw) : base (raw)
        {
        }
    }

    [Hyena.Gui.TestModule ("Animated VBox")]
    internal class AnimatedVBoxTestModule : Gtk.Window
    {
        Label label1, label2, label3;

        public AnimatedVBoxTestModule () : base ("Animated VBox")
        {
            AnimatedVBox vbox = new AnimatedVBox ();
            Add (vbox);
            ShowAll ();

            label1 = new Label ("First Label");
            label2 = new Label ("Second Label");
            label3 = new Label ("Third Label with a longer text");

            vbox.PackEnd (label1, Hyena.Gui.Theatrics.Easing.Linear, Blocking.Downstage);
            vbox.PackEnd (label2, 2000, Hyena.Gui.Theatrics.Easing.ExponentialIn, Blocking.Upstage);
            vbox.PackEnd (label3, 5000, Hyena.Gui.Theatrics.Easing.QuadraticInOut);

            GLib.Timeout.AddSeconds (10, delegate {
                vbox.Remove (label2);
                vbox.Remove (label1);
                vbox.Remove (label3);
                return false;
            });
        }
    }
}