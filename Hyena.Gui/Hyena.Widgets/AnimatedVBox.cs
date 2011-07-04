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
        Table tile, tile2;
        uint timeout_id;

        public AnimatedVBoxTestModule () : base ("Animated VBox")
        {
            AnimatedVBox vbox = new AnimatedVBox ();
            Add (vbox);

            tile = BuildWidget ("Example destroyed");
            tile2 = BuildWidget ("Example removed");

            vbox.PackEnd (tile, 3000, Hyena.Gui.Theatrics.Easing.QuadraticOut);
            vbox.PackEnd (tile2, 3000, Hyena.Gui.Theatrics.Easing.QuadraticOut);
            ShowAll ();

            timeout_id = GLib.Timeout.AddSeconds (5, delegate {
                tile.Destroy ();
                vbox.Remove (tile2);
                return false;
            });
        }

        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
            if (timeout_id > 0) {
                GLib.Timeout.Remove (timeout_id);
            }
            return base.OnDeleteEvent (evnt);
        }


        private Table BuildWidget (string title)
        {
            var tile = new Table (3, 2, false);

            tile.ColumnSpacing = 5;
            tile.RowSpacing = 2;

            var title_label = new Label ();
            title_label.Xalign = 0.0f;
            title_label.Ellipsize = Pango.EllipsizeMode.End;
            title_label.Markup = String.Format ("<small><b>{0}</b></small>", GLib.Markup.EscapeText (title));

            var status_label = new Label ();
            status_label.Xalign = 0.0f;
            status_label.Ellipsize = Pango.EllipsizeMode.End;
            status_label.Markup = "<small>Testing...</small>";

            var progress_bar = new ProgressBar ();
            progress_bar.SetSizeRequest (0, -1);
            progress_bar.Fraction = 0.5;
            progress_bar.Text = "Doing nothing...";

            var cancel_button = new Button (new Image (Stock.Stop, IconSize.Menu));
            cancel_button.Relief = ReliefStyle.None;

            tile.Attach (title_label, 0, 3, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            tile.Attach (status_label, 0, 3, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            tile.Attach (progress_bar, 1, 2, 2, 3,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink, 0, 0);

            tile.Attach (cancel_button, 2, 3, 2, 3,
                AttachOptions.Shrink | AttachOptions.Fill,
                AttachOptions.Shrink | AttachOptions.Fill, 0, 0);

            tile.ShowAll ();

            return tile;
        }
    }
}