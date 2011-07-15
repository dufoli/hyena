//
// Theme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections.Generic;
using Gtk;
using Gdk;
using Cairo;

using Hyena.Gui;

namespace Hyena.Gui.Theming
{
    public abstract class Theme
    {
        private static Cairo.Color black = new Cairo.Color (0, 0, 0);
        private Stack<ThemeContext> contexts = new Stack<ThemeContext> ();

        private Cairo.Color selection_fill;
        private Cairo.Color selection_stroke;

        private Cairo.Color view_fill;
        private Cairo.Color view_fill_transparent;

        private Cairo.Color text_mid;

        public Widget Widget { get; private set; }

        public Theme (Widget widget)
        {
            this.Widget = widget;
            if (widget.IsRealized) {
                OnColorsRefreshed ();
            }

            widget.Realized += delegate { OnColorsRefreshed (); };
            widget.StyleUpdated += delegate { OnColorsRefreshed (); };

            PushContext ();
        }

        protected virtual void OnColorsRefreshed ()
        {
            selection_fill = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Active));
            selection_fill = CairoExtensions.ColorShade (selection_fill, 0.8);
            selection_stroke = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Selected));

            Widget.StyleContext.Save ();
            Widget.StyleContext.AddClass ("entry");
            view_fill = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Normal));
            var text_color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetColor (StateFlags.Normal));
            Widget.StyleContext.Restore ();

            view_fill_transparent = view_fill;
            view_fill_transparent.A = 0;

            text_mid = CairoExtensions.AlphaBlend (view_fill, text_color, 0.5);
        }

#region Drawing

        public abstract void DrawPie (double fraction);

        public void DrawArrow (Cairo.Context cr, Gdk.Rectangle alloc, Hyena.Data.SortType type)
        {
            DrawArrow (cr, alloc, Math.PI / 2.0 * (type == Hyena.Data.SortType.Ascending ? 1 : -1));
        }

        public abstract void DrawArrow (Cairo.Context cr, Gdk.Rectangle alloc, double rotation);

        public void DrawFrame (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
        {
            DrawFrameBackground (cr, alloc, baseColor);
            DrawFrameBorder (cr, alloc);
        }

        public void DrawFrame (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color)
        {
            DrawFrameBackground (cr, alloc, color);
            DrawFrameBorder (cr, alloc);
        }

        public void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
        {
            Cairo.Color fill_color;
            if (baseColor) {
                Widget.StyleContext.Save ();
                Widget.StyleContext.AddClass ("entry");
                fill_color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Normal));
                Widget.StyleContext.Restore ();
            } else {
                fill_color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Normal));
            }

            DrawFrameBackground (cr, alloc, fill_color);
        }

        public void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color)
        {
            DrawFrameBackground (cr, alloc, color, null);
        }

        public void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Pattern pattern)
        {
            DrawFrameBackground (cr, alloc, black , pattern);
        }

        public abstract void DrawFrameBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color, Cairo.Pattern pattern);

        public abstract void DrawFrameBorder (Cairo.Context cr, Gdk.Rectangle alloc);

        public abstract void DrawHeaderBackground (Cairo.Context cr, Gdk.Rectangle alloc);

        public abstract void DrawColumnHeaderFocus (Cairo.Context cr, Gdk.Rectangle alloc);

        public abstract void DrawHeaderSeparator (Cairo.Context cr, Gdk.Rectangle alloc, int x);

        public void DrawListBackground (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
        {
            Cairo.Color fill_color;
            if (baseColor) {
                Widget.StyleContext.Save ();
                Widget.StyleContext.AddClass ("entry");
                fill_color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Normal));
                Widget.StyleContext.Restore ();
            } else {
                fill_color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Normal));
            }
            DrawListBackground (cr, alloc, fill_color);
        }

        public abstract void DrawListBackground (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color);

        public void DrawColumnHighlight (Cairo.Context cr, double cellWidth, double cellHeight)
        {
            Gdk.Rectangle alloc = new Gdk.Rectangle ();
            alloc.Width = (int)cellWidth;
            alloc.Height = (int)cellHeight;
            DrawColumnHighlight (cr, alloc);
        }

        public void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc)
        {
            Cairo.Color color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Selected));
            DrawColumnHighlight (cr, alloc, color);
        }

        public abstract void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color);

        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height)
        {
            DrawRowSelection (cr, x, y, width, height, true);
        }

        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height, bool filled)
        {
            Cairo.Color color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Selected));
            DrawRowSelection (cr, x, y, width, height, filled, true, color, CairoCorners.All);
        }

        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
            bool filled, bool stroked, Cairo.Color color)
        {
            DrawRowSelection (cr, x, y, width, height, filled, stroked, color, CairoCorners.All);
        }

        public void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height)
        {
            Cairo.Color color = CairoExtensions.GdkRGBAToCairoColor (Widget.StyleContext.GetBackgroundColor (StateFlags.Selected));
            DrawRowCursor (cr, x, y, width, height, color);
        }

        public void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height, Cairo.Color color)
        {
            DrawRowCursor (cr, x, y, width, height, color, CairoCorners.All);
        }

        public abstract void DrawRowCursor (Cairo.Context cr, int x, int y, int width, int height, Cairo.Color color, CairoCorners corners);

        public abstract void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
            bool filled, bool stroked, Cairo.Color color, CairoCorners corners);

        public abstract void DrawRowRule (Cairo.Context cr, int x, int y, int width, int height);

        public Cairo.Color ViewFill {
            get { return view_fill; }
        }

        public Cairo.Color ViewFillTransparent {
            get { return view_fill_transparent; }
        }

        public Cairo.Color SelectionFill {
            get { return selection_fill; }
        }

        public Cairo.Color SelectionStroke {
            get { return selection_stroke; }
        }

        public Cairo.Color TextMidColor {
            get { return text_mid; }
            protected set { text_mid = value; }
        }

        public virtual int BorderWidth {
            get { return 1; }
        }

        public virtual int InnerBorderWidth {
            get { return 4; }
        }

        public int TotalBorderWidth {
            get { return BorderWidth + InnerBorderWidth; }
        }

#endregion

#region Contexts

        public virtual void PushContext ()
        {
            PushContext (new ThemeContext ());
        }

        public virtual void PushContext (ThemeContext context)
        {
            lock (this) {
                contexts.Push (context);
            }
        }

        public virtual ThemeContext PopContext ()
        {
            lock (this) {
                return contexts.Pop ();
            }
        }

        public virtual ThemeContext Context {
            get { lock (this) { return contexts.Peek (); } }
        }

#endregion

#region Static Utilities

        public static double Clamp (double min, double max, double value)
        {
             return Math.Max (min, Math.Min (max, value));
        }

#endregion

    }
}
