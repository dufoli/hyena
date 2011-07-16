//
// CompositeUtils.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
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
using System.Runtime.InteropServices;
using Gdk;
using Gtk;

namespace Hyena.Gui
{
    public static class CompositeUtils
    {
        public static bool SetRgbaVisual (Widget w)
        {
            Visual visual = w.Screen.RgbaVisual;

            if (visual != null) {
                w.Visual = visual;
                return true;
            }

            return false;
        }

        [DllImport ("libgdk-win32-2.0-0.dll")]
        private static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type,
            int format, int mode, uint [] data, int nelements);

        [DllImport ("libgdk-win32-2.0-0.dll")]
        private static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type,
            int format, int mode, byte [] data, int nelements);

        public static void ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, uint [] data)
        {
            gdk_property_change (win.Handle, property.Handle, type.Handle, 32, (int)mode,  data, data.Length * 4);
        }

        public static void ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, byte [] data)
        {
            gdk_property_change (win.Handle, property.Handle, type.Handle, 8, (int)mode,  data, data.Length);
        }

        [DllImport ("libgdk-win32-2.0-0.dll")]
        private static extern bool gdk_x11_screen_supports_net_wm_hint (IntPtr screen, IntPtr property);

        public static bool SupportsHint (Screen screen, string name)
        {
            try {
                Atom atom = Atom.Intern (name, false);
                return gdk_x11_screen_supports_net_wm_hint (screen.Handle, atom.Handle);
            } catch {
                return false;
            }
        }

        public static void SetWinOpacity (Gtk.Window win, double opacity)
        {
            CompositeUtils.ChangeProperty (win.Window,
                Atom.Intern ("_NET_WM_WINDOW_OPACITY", false),
                Atom.Intern ("CARDINAL", false),
                PropMode.Replace,
                new uint [] { (uint) (0xffffffff * opacity) }
            );
        }
    }
}
