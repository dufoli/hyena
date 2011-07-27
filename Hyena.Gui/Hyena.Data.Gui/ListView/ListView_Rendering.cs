//
// ListView_Rendering.cs
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

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Gui.Canvas;

namespace Hyena.Data.Gui
{
    public delegate int ListViewRowHeightHandler (Widget widget);

    public partial class ListView<T> : ListViewBase
    {
        private CellContext cell_context;
        private Pango.Layout pango_layout;

        public override Pango.Layout PangoLayout {
            get {
                if (pango_layout == null && Window != null && IsRealized) {
                    using (var cr = Gdk.CairoHelper.Create (Window)) {
                        pango_layout = CairoExtensions.CreateLayout (this, cr);
                        cell_context.FontDescription = pango_layout.FontDescription;
                        cell_context.Layout = pango_layout;
                    }
                }
                return pango_layout;
            }
        }

        public override Pango.FontDescription FontDescription {
            get { return cell_context  != null ? cell_context.FontDescription : null; }
        }

        private List<int> selected_rows = new List<int> ();

        private Theme theme;
        protected Theme Theme {
            get { return theme; }
        }

        // Using an auto-property here makes the build fail with mono 1.9.1 (bnc#396633)
        private bool do_not_render_null_model;
        public bool DoNotRenderNullModel {
            get { return do_not_render_null_model; }
            set { do_not_render_null_model = value; }
        }

        protected override void OnStyleUpdated ()
        {
            base.OnStyleUpdated ();

            // FIXME: legacy list foo
            if (ViewLayout == null) {
                OnInvalidateMeasure ();
            }

            theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (this);

            // Save the drawable so we can reuse it
            var drawable = cell_context != null ? cell_context.Drawable : null;

            if (pango_layout != null) {
                cell_context.FontDescription.Dispose ();
                pango_layout.Dispose ();
                pango_layout = null;
                cell_context.Layout = null;
                cell_context.FontDescription = null;
            }

            cell_context = new CellContext ();
            cell_context.Theme = theme;
            cell_context.Widget = this;
            cell_context.Drawable = drawable;
            SetDirection ();
        }

        private void SetDirection ()
        {
            var dir = Direction;
            if (dir == Gtk.TextDirection.None) {
                dir = Gtk.Widget.DefaultDirection;
            }

            if (cell_context != null) {
                cell_context.IsRtl = dir == Gtk.TextDirection.Rtl;
            }
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            if (DoNotRenderNullModel && Model == null) {
                return true;
            }

            cell_context.Layout = PangoLayout;
            cell_context.Context = cr;

            // FIXME: legacy list foo
            if (ViewLayout == null) {
                OnMeasure ();
            }
            // treview style
            StyleContext.Save ();
            StyleContext.AddClass ("view");

            StyleContext.RenderBackground (cr, 0, 0, Allocation.Width, Allocation.Height);

            // FIXME: ViewLayout will never be null in the future but we'll need
            // to deterministically render a header somehow...
            if (header_visible && ViewLayout == null && column_controller != null) {
                PaintHeader (cr);
            }


            if (Model != null) {
                // FIXME: ViewLayout will never be null in
                // the future, PaintList will go away
                if (ViewLayout == null) {
                    PaintList (cr, new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height));
                } else {
                    PaintView (cr, new Rect (0.0, 0.0, Allocation.Width, Allocation.Height));
                }
            }

            StyleContext.RenderFrame (cr, 0, 0, Allocation.Width, Allocation.Height);

            PaintDraggingColumn (cr);
            StyleContext.Restore ();
            return true;
        }

#region Header Rendering

        private void PaintHeader (Cairo.Context cr)
        {
            Rectangle clip = header_rendering_alloc;
            clip.Height += Theme.BorderWidth;
            clip.Intersect (new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height));
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();

            Theme.DrawHeaderBackground (cr, header_rendering_alloc);

            Rectangle cell_area = new Rectangle ();
            cell_area.Y = header_rendering_alloc.Y;
            cell_area.Height = header_rendering_alloc.Height;

            cell_context.Clip = clip;
            cell_context.Opaque = true;
            cell_context.TextAsForeground = true;

            bool have_drawn_separator = false;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }

                cell_area.X = column_cache[ci].X1 + Theme.TotalBorderWidth + header_rendering_alloc.X - HadjustmentValue;
                cell_area.Width = column_cache[ci].Width;
                PaintHeaderCell (cr, cell_area, ci, false, ref have_drawn_separator);
            }

            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.X = pressed_column_x_drag - HadjustmentValue;
                cell_area.Width = column_cache[pressed_column_index].Width;
                PaintHeaderCell (cr, cell_area, pressed_column_index, true, ref have_drawn_separator);
            }

            cr.ResetClip ();
        }

        private void PaintHeaderCell (Cairo.Context cr, Rectangle area, int ci, bool dragging, ref bool have_drawn_separator)
        {
            if (ci < 0 || column_cache.Length <= ci)
                return;

            if (ci == ActiveColumn && HasFocus && HeaderFocused) {
                Theme.DrawColumnHeaderFocus (cr, area);
            }

            if (dragging) {
                Cairo.Color dark_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Normal));
                dark_color = CairoExtensions.ColorShade (dark_color, 0.7);

                Theme.DrawColumnHighlight (cr, area, dark_color);

                StyleContext.Save ();
                StyleContext.AddClass ("entry");
                Cairo.Color base_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Normal));
                StyleContext.Restore ();

                Cairo.Color stroke_color = CairoExtensions.ColorShade (base_color, 0.0);
                stroke_color.A = 0.3;

                cr.Color = stroke_color;
                cr.MoveTo (area.X + 0.5, area.Y + 1.0);
                cr.LineTo (area.X + 0.5, area.Bottom);
                cr.MoveTo (area.Right - 0.5, area.Y + 1.0);
                cr.LineTo (area.Right - 0.5, area.Bottom);
                cr.Stroke ();
            }

            ColumnCell cell = column_cache[ci].Column.HeaderCell;

            if (cell != null) {
                cr.Save ();
                cr.Translate (area.X, area.Y);
                cell_context.Area = area;
                cell_context.State = StateFlags.Normal;
                cell.Render (cell_context, area.Width, area.Height);
                cr.Restore ();
            }

            if (!dragging && ci < column_cache.Length - 1 && (have_drawn_separator ||
                column_cache[ci].MaxWidth != column_cache[ci].MinWidth)) {
                have_drawn_separator = true;
                Theme.DrawHeaderSeparator (cr, area, area.Right);
            }
        }

#endregion

#region List Rendering

        private void PaintList (Cairo.Context cr, Rectangle clip)
        {
            if (ChildSize.Height <= 0) {
                return;
            }

            // TODO factor this out?
            // Render the sort effect to the GdkWindow.
            if (sort_column_index != -1 && (!pressed_column_is_dragging || pressed_column_index != sort_column_index)) {
                CachedColumn col = column_cache[sort_column_index];
                StyleContext.AddRegion ("column", RegionFlags.Sorted);
                StyleContext.RenderBackground (cr,
                    list_rendering_alloc.X + col.X1 - HadjustmentValue,
                    header_rendering_alloc.Bottom + Theme.BorderWidth,
                    col.Width, list_rendering_alloc.Height + Theme.InnerBorderWidth * 2);
                StyleContext.RemoveRegion ("column");
            }

            clip.Intersect (list_rendering_alloc);
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();

            cell_context.Clip = clip;
            cell_context.TextAsForeground = false;

            int vadjustment_value = VadjustmentValue;
            int first_row = vadjustment_value / ChildSize.Height;
            int last_row = Math.Min (model.Count, first_row + RowsInView);
            int offset = list_rendering_alloc.Y - vadjustment_value % ChildSize.Height;

            Rectangle selected_focus_alloc = Rectangle.Zero;
            Rectangle single_list_alloc = new Rectangle ();

            single_list_alloc.X = list_rendering_alloc.X - HadjustmentValue;
            single_list_alloc.Y = offset;
            single_list_alloc.Width = list_rendering_alloc.Width + HadjustmentValue;
            single_list_alloc.Height = ChildSize.Height;

            int selection_height = 0;
            int selection_y = 0;
            selected_rows.Clear ();

            for (int ri = first_row; ri < last_row; ri++) {
                if (Selection != null && Selection.Contains (ri)) {
                    if (selection_height == 0) {
                        selection_y = single_list_alloc.Y;
                    }

                    selection_height += single_list_alloc.Height;
                    selected_rows.Add (ri);

                    if (Selection.FocusedIndex == ri) {
                        selected_focus_alloc = single_list_alloc;
                    }
                } else {
                    if (rules_hint && ri % 2 != 0) {
                        StyleContext.AddRegion ("row", RegionFlags.Odd);
                        StyleContext.RenderBackground (cr, single_list_alloc.X, single_list_alloc.Y,
                            single_list_alloc.Width, single_list_alloc.Height);
                        StyleContext.RemoveRegion ("row");
                    }

                    PaintReorderLine (cr, ri, single_list_alloc);

                    if (Selection != null && Selection.FocusedIndex == ri && !Selection.Contains (ri) && HasFocus) {
                        CairoCorners corners = CairoCorners.All;

                        if (Selection.Contains (ri - 1)) {
                            corners &= ~(CairoCorners.TopLeft | CairoCorners.TopRight);
                        }

                        if (Selection.Contains (ri + 1)) {
                            corners &= ~(CairoCorners.BottomLeft | CairoCorners.BottomRight);
                        }

                        if (HasFocus && !HeaderFocused) // Cursor out of selection.
                            Theme.DrawRowCursor (cr, single_list_alloc.X, single_list_alloc.Y,
                                                 single_list_alloc.Width, single_list_alloc.Height,
                                                 CairoExtensions.ColorShade (CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Selected)), 0.85));
                    }

                    if (selection_height > 0) {
                        Cairo.Color selection_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Selected));

                        if (!HasFocus || HeaderFocused)
                            selection_color = CairoExtensions.ColorShade (selection_color, 1.1);

                        Theme.DrawRowSelection (cr, list_rendering_alloc.X, selection_y, list_rendering_alloc.Width, selection_height,
                                                true, true, selection_color, CairoCorners.All);
                        selection_height = 0;
                    }

                    PaintRow (cr, ri, single_list_alloc, StateFlags.Normal);
                }

                single_list_alloc.Y += single_list_alloc.Height;
            }

            // In case the user is dragging to the end of the list
            PaintReorderLine (cr, last_row, single_list_alloc);

            if (selection_height > 0) {
                Theme.DrawRowSelection (cr, list_rendering_alloc.X, selection_y,
                    list_rendering_alloc.Width, selection_height);
            }

            if (Selection != null && Selection.Count > 1 &&
                !selected_focus_alloc.Equals (Rectangle.Zero) &&
                HasFocus && !HeaderFocused) { // Cursor inside selection.
                // Use entry to get text color
                StyleContext.Save ();
                StyleContext.AddClass ("entry");
                Cairo.Color text_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetColor (StateFlags.Selected));
                StyleContext.Restore ();

                Theme.DrawRowCursor (cr, selected_focus_alloc.X, selected_focus_alloc.Y,
                    selected_focus_alloc.Width, selected_focus_alloc.Height, text_color);
            }

            foreach (int ri in selected_rows) {
                single_list_alloc.Y = offset + ((ri - first_row) * single_list_alloc.Height);
                PaintRow (cr, ri, single_list_alloc, StateFlags.Selected);
            }

            cr.ResetClip ();
        }

        private void PaintReorderLine (Cairo.Context cr, int row_index, Rectangle single_list_alloc)
        {
            if (row_index == drag_reorder_row_index && IsReorderable) {
                cr.Save ();
                cr.LineWidth = 1.0;
                cr.Antialias = Cairo.Antialias.None;
                cr.MoveTo (single_list_alloc.Left, single_list_alloc.Top);
                cr.LineTo (single_list_alloc.Right, single_list_alloc.Top);

                StyleContext.Save ();
                StyleContext.AddClass ("entry");
                cr.Color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetColor (StateFlags.Normal));
                StyleContext.Restore ();

                cr.Stroke ();
                cr.Restore ();
            }
        }

        private void PaintRow (Cairo.Context cr, int row_index, Rectangle area, StateFlags state)
        {
            if (column_cache == null) {
                return;
            }

            object item = model[row_index];
            bool opaque = IsRowOpaque (item);
            bool bold = IsRowBold (item);

            Rectangle cell_area = new Rectangle ();
            cell_area.Height = ChildSize.Height;
            cell_area.Y = area.Y;

            cell_context.ViewRowIndex = cell_context.ModelRowIndex = row_index;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                cell_context.ViewColumnIndex = ci;

                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }

                cell_area.Width = column_cache[ci].Width;
                cell_area.X = column_cache[ci].X1 + area.X;
                PaintCell (cr, item, ci, row_index, cell_area, opaque, bold, state, false);
            }

            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.Width = column_cache[pressed_column_index].Width;
                cell_area.X = pressed_column_x_drag + list_rendering_alloc.X -
                    list_interaction_alloc.X - HadjustmentValue;
                PaintCell (cr, item, pressed_column_index, row_index, cell_area, opaque, bold, state, true);
            }
        }

        private void PaintCell (Cairo.Context cr, object item, int column_index, int row_index, Rectangle area, bool opaque, bool bold,
            StateFlags state, bool dragging)
        {
            ColumnCell cell = column_cache[column_index].Column.GetCell (0);
            cell.Bind (item);
            ColumnCellDataProvider (cell, item);

            ITextCell text_cell = cell as ITextCell;
            if (text_cell != null) {
                text_cell.FontWeight = bold ? Pango.Weight.Bold : Pango.Weight.Normal;
            }

            if (dragging) {
                StyleContext.Save ();
                StyleContext.AddClass ("entry");
                Cairo.Color fill_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Normal));
                StyleContext.Restore ();
                fill_color.A = 0.5;
                cr.Color = fill_color;
                cr.Rectangle (area.X, area.Y, area.Width, area.Height);
                cr.Fill ();
            }

            cr.Save ();
            cr.Translate (area.X, area.Y);
            cell_context.Area = area;
            cell_context.Opaque = opaque;
            cell_context.State = dragging ? StateFlags.Normal : state;
            cell.Render (cell_context, area.Width, area.Height);
            cr.Restore ();

            AccessibleCellRedrawn (column_index, row_index);
        }

        private void PaintDraggingColumn (Cairo.Context cr)
        {
            if (!pressed_column_is_dragging || pressed_column_index < 0) {
                return;
            }

            CachedColumn column = column_cache[pressed_column_index];

            int x = pressed_column_x_drag + 1 - HadjustmentValue;

            StyleContext.Save ();
            StyleContext.AddClass ("entry");
            Cairo.Color fill_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Normal));

            Cairo.Color stroke_color = CairoExtensions.ColorShade (fill_color, 0.0);
            fill_color.A = 0.45;
            stroke_color.A = 0.3;
            StyleContext.Restore ();

            cr.Rectangle (x, header_rendering_alloc.Bottom + 1, column.Width - 2,
                list_rendering_alloc.Bottom - header_rendering_alloc.Bottom - 1);
            cr.Color = fill_color;
            cr.Fill ();

            cr.MoveTo (x - 0.5, header_rendering_alloc.Bottom + 0.5);
            cr.LineTo (x - 0.5, list_rendering_alloc.Bottom + 0.5);
            cr.LineTo (x + column.Width - 1.5, list_rendering_alloc.Bottom + 0.5);
            cr.LineTo (x + column.Width - 1.5, header_rendering_alloc.Bottom + 0.5);

            cr.Color = stroke_color;
            cr.LineWidth = 1.0;
            cr.Stroke ();
        }

#endregion

#region View Layout Rendering

        private void PaintView (Cairo.Context cr, Rect clip)
        {
            clip.Intersect ((Rect)list_rendering_alloc);
            cr.Rectangle ((Cairo.Rectangle)clip);
            cr.Clip ();

            cell_context.Clip = (Gdk.Rectangle)clip;
            cell_context.TextAsForeground = false;

            selected_rows.Clear ();

            for (int layout_index = 0; layout_index < ViewLayout.ChildCount; layout_index++) {
                var layout_child = ViewLayout[layout_index];
                var child_allocation = layout_child.Allocation;

                if (!child_allocation.IntersectsWith (clip) || ViewLayout.GetModelIndex (layout_child) >= Model.Count) {
                    continue;
                }

                if (Selection != null && Selection.Contains (ViewLayout.GetModelIndex (layout_child))) {
                    selected_rows.Add (ViewLayout.GetModelIndex (layout_child));

                    var selection_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Selected));
                    if (!HasFocus || HeaderFocused) {
                        selection_color = CairoExtensions.ColorShade (selection_color, 1.1);
                    }

                    Theme.DrawRowSelection (cr,
                        (int)child_allocation.X, (int)child_allocation.Y,
                        (int)child_allocation.Width, (int)child_allocation.Height,
                        true, true, selection_color, CairoCorners.All);

                    cell_context.State = StateFlags.Selected;
                } else {
                    cell_context.State = StateFlags.Normal;
                }

                //cr.Save ();
                //cr.Translate (child_allocation.X, child_allocation.Y);
                //cr.Rectangle (0, 0, child_allocation.Width, child_allocation.Height);
                //cr.Clip ();
                layout_child.Render (cell_context);
                //cr.Restore ();
            }

            cr.ResetClip ();
        }

#endregion

        protected void InvalidateList ()
        {
            if (IsRealized) {
                QueueDirtyRegion (list_rendering_alloc);
            }
        }

        private void InvalidateHeader ()
        {
            if (IsRealized) {
                QueueDirtyRegion (header_rendering_alloc);
            }
        }

        protected void QueueDirtyRegion ()
        {
            QueueDirtyRegion (list_rendering_alloc);
        }

        protected virtual void ColumnCellDataProvider (ColumnCell cell, object boundItem)
        {
        }

        private bool rules_hint = false;
        public bool RulesHint {
            get { return rules_hint; }
            set {
                rules_hint = value;
                InvalidateList ();
            }
        }

// FIXME: Obsolete all this measure stuff on the view since it's in the layout
#region Measuring

        private Gdk.Size child_size = Gdk.Size.Empty;
        public Gdk.Size ChildSize {
            get {
                return ViewLayout != null
                    ? new Gdk.Size ((int)ViewLayout.ChildSize.Width, (int)ViewLayout.ChildSize.Height)
                    : child_size;
            }
        }

        private bool measure_pending;

        protected virtual void OnInvalidateMeasure ()
        {
            measure_pending = true;
            if (IsMapped && IsRealized) {
                QueueDirtyRegion ();
            }
        }

        protected virtual Gdk.Size OnMeasureChild ()
        {
            return ViewLayout != null
                ? new Gdk.Size ((int)ViewLayout.ChildSize.Width, (int)ViewLayout.ChildSize.Height)
                : new Gdk.Size (0, ColumnCellText.ComputeRowHeight (this));
        }

        private void OnMeasure ()
        {
            if (!measure_pending) {
                return;
            }

            measure_pending = false;

            header_height = 0;
            child_size = OnMeasureChild ();
            UpdateAdjustments ();
        }

#endregion

    }
}
