//
// AnimatedWidget.cs
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
using System.Collections.Generic;
using Gdk;
using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theatrics;

namespace Hyena.Widgets
{
    internal enum AnimationState
    {
        Coming,
        Idle,
        IntendingToGo,
        Going
    }

    internal class AnimatedWidget : Container
    {
        public event EventHandler WidgetDestroyed;

        public Widget Widget;
        public Easing Easing;
        public Blocking Blocking;
        public AnimationState AnimationState;
        public uint Duration;
        public double Bias = 1.0;
        public int Width;
        public int Height;
        public int StartPadding;
        public int EndPadding;
        public LinkedListNode <AnimatedWidget> Node;

        private readonly bool horizontal;
        private double percent;
        private Rectangle widget_alloc;
        private Cairo.Surface surface;

        public AnimatedWidget (Widget widget, uint duration, Easing easing, Blocking blocking, bool horizontal)
        {
            this.horizontal = horizontal;
            Widget = widget;
            Duration = duration;
            Easing = easing;
            Blocking = blocking;
            AnimationState = AnimationState.Coming;

            Widget.Parent = this;
            Widget.Destroyed += OnWidgetDestroyed;
            ShowAll ();
        }

        protected AnimatedWidget (IntPtr raw) : base (raw)
        {
        }

        public double Percent {
            get { return percent; }
            set {
                percent = value * Bias;
                QueueResizeNoRedraw ();
            }
        }

        private void OnWidgetDestroyed (object sender, EventArgs args)
        {
            if (!IsRealized) {
                return;
            }

            // Copy the widget's pixels to surface, we'll use it to draw the animation
            surface = Window.CreateSimilarSurface (Cairo.Content.ColorAlpha, widget_alloc.Width, widget_alloc.Height);
            var cr = new Cairo.Context (surface);
            Gdk.CairoHelper.SetSourceWindow (cr, Window, widget_alloc.X, widget_alloc.Y);
            cr.Rectangle (0, 0, widget_alloc.Width, widget_alloc.Height);
            cr.Fill ();

            if (AnimationState != AnimationState.Going) {
                WidgetDestroyed (this, args);
            }

            ((IDisposable)cr).Dispose ();
        }

#region Overrides

        protected override void OnRemoved (Widget widget)
        {
            if (widget == Widget) {
                widget.Unparent ();
                Widget = null;
            }
        }

        protected override void OnRealized ()
        {
            IsRealized = true;

            Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.Wclass = Gdk.WindowWindowClass.Output;
            attributes.EventMask = (int)Gdk.EventMask.ExposureMask;

            Window = new Gdk.Window (Parent.Window, attributes, 0);
            Window.UserData = Handle;
            Window.BackgroundRgba = StyleContext.GetBackgroundColor (StateFlags);
        }

        protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
        {
            var requisition = SizeRequested ();
            minimum_height = natural_height = requisition.Height;
        }

        protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
        {
            var requisition = SizeRequested ();
            minimum_width = natural_width = requisition.Width;
        }

        protected Requisition SizeRequested ()
        {
            var requisition = new Requisition ();
            if (Widget != null) {
                Requisition req, nat;
                Widget.GetPreferredSize (out req, out nat);
                widget_alloc.Width = req.Width;
                widget_alloc.Height = req.Height;
            }

            if (horizontal) {
                Width = Choreographer.PixelCompose (percent, widget_alloc.Width + StartPadding + EndPadding, Easing);
                Height = widget_alloc.Height;
            } else {
                Width = widget_alloc.Width;
                Height = Choreographer.PixelCompose (percent, widget_alloc.Height + StartPadding + EndPadding, Easing);
            }

            requisition.Width = Width;
            requisition.Height = Height;
            return requisition;
        }

        protected override void OnSizeAllocated (Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            if (Widget != null) {
                if (horizontal) {
                    widget_alloc.Height = allocation.Height;
                    widget_alloc.X = StartPadding;
                    if (Blocking == Blocking.Downstage) {
                        widget_alloc.X += allocation.Width - widget_alloc.Width;
                    }
                } else {
                    widget_alloc.Width = allocation.Width;
                    widget_alloc.Y = StartPadding;
                    if (Blocking == Blocking.Downstage) {
                        widget_alloc.Y = allocation.Height - widget_alloc.Height;
                    }
                }

                if (widget_alloc.Height > 0 && widget_alloc.Width > 0) {
                    Widget.SizeAllocate (widget_alloc);
                }
            }
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            if (surface != null) {
                cr.Save ();
                Gtk.CairoHelper.TransformToWindow (cr, this, Window);
                cr.SetSource (surface);
                cr.Rectangle (widget_alloc.X, widget_alloc.Y, widget_alloc.Width, widget_alloc.Height);
                cr.Fill ();
                cr.Restore ();
                return true;
            } else {
                return base.OnDrawn (cr);
            }
        }

        protected override void ForAll (bool include_internals, Callback callback)
        {
            if (Widget != null) {
                callback (Widget);
            }
        }

#endregion

    }
}
