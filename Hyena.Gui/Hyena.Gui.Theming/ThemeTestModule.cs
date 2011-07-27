// 
// ThemeTestModule.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright  2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;

using Hyena.Gui;

namespace Hyena.Gui.Theming
{
    [TestModule ("Theme")]
    public class ThemeTestModule : Window
    {
        public ThemeTestModule () : base ("Theme")
        {
            var align = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
            var theme_widget = new ThemeTestWidget ();
            align.Add (theme_widget);
            Add (align);
            ShowAll ();

            int state = 0;
            uint[,] borders = {
                {0, 0, 0, 0},
                {10, 0, 0, 0},
                {0, 10, 0, 0},
                {0, 0, 10, 0},
                {0, 0, 0, 10},
                {10, 10, 0, 0},
                {10, 10, 10, 0},
                {10, 10, 10, 10},
                {10, 0, 0, 10},
                {0, 10, 10, 0}
            };

            GLib.Timeout.Add (2000, delegate {
                Console.WriteLine (state);
                align.TopPadding = borders[state, 0];
                align.RightPadding = borders[state, 1];
                align.BottomPadding = borders[state, 2];
                align.LeftPadding = borders[state, 3];
                if (++state % borders.GetLength (0) == 0) {
                    state = 0;
                }
                return true;
            });
        }

        private class ThemeTestWidget : DrawingArea
        {
            private Theme theme;

            protected override void OnStyleUpdated ()
            {
                base.OnStyleUpdated ();
                theme = ThemeEngine.CreateTheme (this);
                theme.Context.Radius = 10;
            }

            protected override bool OnDrawn (Cairo.Context cr)
            {
                var alloc = new Gdk.Rectangle () {
                    X = 0,
                    Y = 0,
                    Width = Allocation.Width,
                    Height = Allocation.Height
                };

                theme.DrawListBackground (cr, alloc, true);
                theme.DrawFrameBorder (cr, alloc);
                return true;
            }
        }
    }
}
