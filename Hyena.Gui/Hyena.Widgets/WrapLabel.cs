//
// WrapLabel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Gtk;

namespace Hyena.Widgets
{
    public class WrapLabel : Widget
    {
        private string text;
        private bool use_markup = false;
        private bool wrap = true;
        private Pango.Layout layout;

        public WrapLabel ()
        {
            HasWindow = false;
        }

        private void CreateLayout ()
        {
            if (layout != null) {
                layout.Dispose ();
            }

            layout = new Pango.Layout (PangoContext);
            layout.Wrap = Pango.WrapMode.Word;
        }

        private void UpdateLayout ()
        {
            if (layout == null) {
                CreateLayout ();
            }

            layout.Ellipsize = wrap ? Pango.EllipsizeMode.None : Pango.EllipsizeMode.End;

            if (text == null) {
                text = "";
            }

            if (use_markup) {
                layout.SetMarkup (text);
            } else {
                layout.SetText (text);
            }

            QueueResize ();
        }

        protected override void OnStyleUpdated ()
        {
            CreateLayout ();
            UpdateLayout ();
            base.OnStyleUpdated ();
        }

        protected override void OnRealized ()
        {
            Window = Parent.Window;
            base.OnRealized ();
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            int lw, lh;

            layout.Width = (int)(allocation.Width * Pango.Scale.PangoScale);
            layout.GetPixelSize (out lw, out lh);

            TooltipText = layout.IsEllipsized ? text : null;
            HeightRequest = lh;

            base.OnSizeAllocated (allocation);
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            if (CairoHelper.ShouldDrawWindow (cr, Window)) {
                // Center the text vertically
                int lw, lh;
                layout.GetPixelSize (out lw, out lh);
                int y = (Allocation.Height - lh) / 2;

                //TODO include in a utils class or find one in gtk
                switch (State) {
                    case StateType.Active:
                        StyleContext.State = StateFlags.Active;
                    break;
                    case StateType.Focused:
                        StyleContext.State = StateFlags.Selected;
                    break;
                    case StateType.Inconsistent:
                        StyleContext.State = StateFlags.Inconsistent;
                    break;
                    case StateType.Insensitive:
                        StyleContext.State = StateFlags.Insensitive;
                    break;
                    case StateType.Normal:
                        StyleContext.State = StateFlags.Normal;
                    break;
                    case StateType.Prelight:
                        StyleContext.State = StateFlags.Prelight;
                    break;
                    case StateType.Selected:
                        StyleContext.State = StateFlags.Selected;
                    break;
                }

                StyleContext.RenderLayout (cr, 0, y, layout);
                //Gtk.Style.PaintLayout (Style, cr, State, false,
                //    this, null, Allocation.X, y, layout);
                cr.Restore ();
            }

            return true;
        }

        public void MarkupFormat (string format, params object [] args)
        {
            if (args == null || args.Length == 0) {
                Markup = format;
                return;
            }

            for (int i = 0; i < args.Length; i++) {
                if (args[i] is string) {
                    args[i] = GLib.Markup.EscapeText ((string)args[i]);
                }
            }

            Markup = String.Format (format, args);
        }

        public bool Wrap {
            get { return wrap; }
            set {
                wrap = value;
                UpdateLayout ();
            }
        }

        public string Markup {
            get { return text; }
            set {
                use_markup = true;
                text = value;
                UpdateLayout ();
            }
        }

        public string Text {
            get { return text; }
            set {
                use_markup = false;
                text = value;
                UpdateLayout ();
            }
        }
    }
}
