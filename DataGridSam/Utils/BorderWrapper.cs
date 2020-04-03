﻿using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    internal class BorderWrapper
    {
        private DataGrid DataGrid;

        internal BoxView left;
        internal BoxView top;
        internal BoxView right;
        internal BoxView bottom;
        internal BoxView absoluteBottom;
        internal BoxView leftScroll;
        internal BoxView rightScroll;

        public BorderWrapper(DataGrid parent)
        {
            DataGrid = parent;
            left = new BoxView();
            left.HorizontalOptions = LayoutOptions.StartAndExpand;
            left.VerticalOptions = LayoutOptions.FillAndExpand;
            left.SetBinding(BoxView.WidthRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));
            left.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));

            top = new BoxView();
            top.HorizontalOptions = LayoutOptions.FillAndExpand;
            top.VerticalOptions = LayoutOptions.StartAndExpand;
            top.SetBinding(BoxView.HeightRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));
            top.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));

            right = new BoxView();
            right.HorizontalOptions = LayoutOptions.EndAndExpand;
            right.VerticalOptions = LayoutOptions.FillAndExpand;
            right.SetBinding(BoxView.WidthRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));
            right.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));

            absoluteBottom = new BoxView();
            absoluteBottom.IsVisible = false;
            absoluteBottom.HorizontalOptions = LayoutOptions.FillAndExpand;
            absoluteBottom.VerticalOptions = LayoutOptions.EndAndExpand;
            absoluteBottom.SetBinding(BoxView.HeightRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));
            absoluteBottom.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));
            Grid.SetRow(absoluteBottom, 1);

            // Bottom line
            bottom = new BoxView();
            bottom.InputTransparent = true;
            bottom.VerticalOptions = LayoutOptions.EndAndExpand;
            bottom.HorizontalOptions = LayoutOptions.FillAndExpand;
            bottom.SetBinding(BoxView.HeightRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));
            bottom.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));
            //Grid.SetRow(bottom, 1);

            // Wrapp borders
            leftScroll = new BoxView();
            leftScroll.VerticalOptions = LayoutOptions.FillAndExpand;
            leftScroll.HorizontalOptions = LayoutOptions.StartAndExpand;
            leftScroll.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));
            leftScroll.SetBinding(BoxView.WidthRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));

            rightScroll = new BoxView();
            rightScroll.VerticalOptions = LayoutOptions.FillAndExpand;
            rightScroll.HorizontalOptions = LayoutOptions.EndAndExpand;
            rightScroll.SetBinding(BoxView.BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: DataGrid));
            rightScroll.SetBinding(BoxView.WidthRequestProperty, new Binding(nameof(DataGrid.BorderWidth), source: DataGrid));
        }

        internal void Update()
        {
            if (DataGrid.IsWrapped)
            {
                // Header
                DataGrid.Children.Add(left);
                DataGrid.Children.Add(top);
                DataGrid.Children.Add(right);
                DataGrid.Children.Add(absoluteBottom);
                DataGrid.bodyGrid.Mask.Children.Add(leftScroll);
                DataGrid.bodyGrid.Mask.Children.Add(rightScroll);
                DataGrid.bodyGrid.Children.Add(bottom);
                DataGrid.bodyGrid.SizeChanged += DataGrid.CheckWrapperBottomVisible;
                DataGrid.mainScroll.SizeChanged += DataGrid.CheckWrapperBottomVisible;

                Grid.SetColumn(rightScroll, DataGrid.Columns?.Count-1 ?? 0);
            }
            else
            {
                DataGrid.Children.Remove(left);
                DataGrid.Children.Remove(top);
                DataGrid.Children.Remove(right);
                DataGrid.Children.Remove(absoluteBottom);
                DataGrid.bodyGrid.Children.Remove(bottom);
                DataGrid.bodyGrid.Mask.Children.Remove(leftScroll);
                DataGrid.bodyGrid.Mask.Children.Remove(rightScroll);
                DataGrid.bodyGrid.SizeChanged -= DataGrid.CheckWrapperBottomVisible;
                DataGrid.mainScroll.SizeChanged -= DataGrid.CheckWrapperBottomVisible;
            }
        }
    }
}
