using DataGridSam.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
//using TouchSam;
//using DataGridSam.Platform;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    public sealed class Row
    {
        public double Height { get; private set; }
        public int Index { get; private set; }
        public bool IsSelected { get; internal set; }

        public readonly object Context;
        internal readonly DataGrid DataGrid;
        internal readonly GridBody GridBody;

        internal readonly List<GridCell> cells = new List<GridCell>();
        internal readonly BoxView touchContainer;
        internal readonly BoxView selectionContainer;
        internal readonly BoxView line;
        internal RowTrigger enableTrigger;


        public Row(object context, GridBody host, int id, int itemsCount)
        {
            Context = context;
            GridBody = host;
            DataGrid = host.DataGrid;
            Index = id;

            // Triggers event
            if (Context is INotifyPropertyChanged model)
                model.PropertyChanged += (obj, e) => RowTrigger.SetTriggerStyle(this, e.PropertyName);

            // Create touch container
            touchContainer = new TouchBox(this);
            Grid.SetColumnSpan(touchContainer, DataGrid.Columns.Count);
            Grid.SetRow(touchContainer, Index);
            GridBody.Children.Add(touchContainer);

            // Create selected container
            selectionContainer = CreateSelectionContainer();
            Grid.SetColumnSpan(selectionContainer, DataGrid.Columns.Count);
            Grid.SetRow(selectionContainer, Index);
            GridBody.Children.Add(selectionContainer);

            // Init cells
            int i = 0;
            foreach (var column in DataGrid.Columns)
            {
                var cell = new GridCell(this, column);
                cells.Add(cell);

                // Detect auto number cell
                cell.AutoNumber = column.AutoNumber;

                // Create custom template
                if (column.CellTemplate != null)
                {
                    cell.View = column.CellTemplate.CreateContent() as View;
                    cell.View.IsVisible = column.IsVisible;
                    cell.View.InputTransparent = true;
                    cell.View.BindingContext = Context;
                    cell.IsCustomTemplate = true;

                    if (cell.View is Layout layout)
                    {
                        layout.IsClippedToBounds = true;
                        layout.InputTransparent = true;
                        layout.CascadeInputTransparent = true;
                    }
                }
                // Create standart cell
                else
                {
                    var label = new LabelExt(this, cell)
                    {
                        IsVisible = column.IsVisible,
                        Margin = DataGrid.CellPadding,
                        //VerticalOptions = LayoutOptions.StartAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                    };

                    // Set binding text with ItemsSource model
                    if (column.PropertyName != null && cell.AutoNumber == Enums.AutoNumberType.None)
                        label.SetBinding(Label.TextProperty, new Binding(
                            column.PropertyName,
                            BindingMode.Default,
                            stringFormat: column.StringFormat,
                            source: Context));

                    cell.View = label;
                }

                // event size
                //cell.InitSizeChanged();

                GridBody.Children.Add(cell.View, i, Index);
                i++;
            }

            // Create horizontal line table
            line = CreateHorizontalLine();
            Grid.SetColumnSpan(line, DataGrid.Columns.Count);
            Grid.SetRow(line, Index);
            GridBody.Children.Add(line);

            // Add tap system event
            Touch.SetSelect(touchContainer, new Command(ActionRowSelect));

            if (DataGrid.TapColor != Color.Default)
                Touch.SetColor(touchContainer, DataGrid.TapColor);

            if (DataGrid.CommandSelectedItem != null)
                Touch.SetTap(touchContainer, DataGrid.CommandSelectedItem);

            if (DataGrid.CommandLongTapItem != null)
                Touch.SetLongTap(touchContainer, DataGrid.CommandLongTapItem);

            // Auto number
            if (DataGrid.IsAutoNumberCalc)
                UpdateAutoNumeric(Index + 1, itemsCount);

            // Started find FIRST active trigger
            if (this.DataGrid.RowTriggers.Count > 0)
                foreach (var trigg in this.DataGrid.RowTriggers)
                    if (trigg.CheckTriggerActivated(Context))
                    {
                        this.enableTrigger = trigg;
                        break;
                    }

            // Render style
            UpdateStyle();
        }

        internal void SolveSizeRow(GridCell cell, double height)
        {
            if (height == Height)
                return;

            foreach (var c in cells)
            {
                if (Height > c.View.Height)
                    height = c.View.Height;
            }

            Height = height;
            if (Height >= 0)
            {
                double add = DataGrid.CellPadding.Bottom + DataGrid.CellPadding.Top;
                cell.IsSizeDone = false;
                //GridBody.RowDefinitions[Index] = new RowDefinition { Height = new GridLength(Height) };
                GridBody.RowDefinitions[Index].Height = new GridLength(Height + add);
            }
        }


        internal void Remove()
        {
            GridBody.RowDefinitions.Remove(GridBody.RowDefinitions.LastOrDefault());
            
            GridBody.Children.Remove(line);
            GridBody.Children.Remove(touchContainer);
            GridBody.Children.Remove(selectionContainer);
            foreach (var cell in cells)
            {
                GridBody.Children.Remove(cell.View);
            }

            GridBody.Rows.Remove(this);
        }

        private void ActionRowSelect(object param)
        {
            var rowTapped = this;
            var lastTapped = DataGrid.SelectedRow;

            // GUI Unselected last row
            if (lastTapped != null && lastTapped != rowTapped)
            {
                lastTapped.IsSelected = false;
                lastTapped.UpdateStyle();
            }

            // GUI Selected row
            if (lastTapped != rowTapped)
            {
                DataGrid.SelectedRow = rowTapped;
                DataGrid.SelectedItem = Context;

                rowTapped.IsSelected = true;
                rowTapped.UpdateStyle();
            }
        }

        internal void UpdateStyle()
        {
            // Selected
            if (IsSelected)
            {
                var color = ValueSelector.GetSelectedColor(
                    DataGrid.VisualSelectedRowFromStyle.BackgroundColor,
                    DataGrid.VisualSelectedRow.BackgroundColor);

                selectionContainer.BackgroundColor = color;
            }
            else
            {
                selectionContainer.BackgroundColor = Color.Transparent;
            }

            // row background
            if (enableTrigger != null)
            {
                // row background
                touchContainer.BackgroundColor = ValueSelector.GetBackgroundColor(
                    enableTrigger.VisualContainerStyle.BackgroundColor,
                    enableTrigger.VisualContainer.BackgroundColor,

                    DataGrid.VisualRowsFromStyle.BackgroundColor,
                    DataGrid.VisualRows.BackgroundColor);
            }
            else
            {
                touchContainer.BackgroundColor = ValueSelector.GetBackgroundColor(
                    DataGrid.VisualRowsFromStyle.BackgroundColor,
                    DataGrid.VisualRows.BackgroundColor);
            }

            // Priority:
            // 1) selected
            // 2) trigger
            // 3) column
            // 4) default
            foreach (var cell in cells)
            {
                if (cell.IsCustomTemplate)
                    continue;

                if (IsSelected)
                {
                    // SELECT
                    if (enableTrigger == null)
                    {
                        MergeVisual(cell.Label,
                            DataGrid.VisualSelectedRowFromStyle,
                            DataGrid.VisualSelectedRow,
                            cell.Column.VisualCellFromStyle,
                            cell.Column.VisualCell,
                            DataGrid.VisualRowsFromStyle,
                            DataGrid.VisualRows);
                    }
                    // SELECT with TRIGGER
                    else
                    {
                        MergeVisual(cell.Label,
                            DataGrid.VisualSelectedRowFromStyle,
                            DataGrid.VisualSelectedRow,
                            enableTrigger.VisualContainerStyle,
                            enableTrigger.VisualContainer,
                            cell.Column.VisualCellFromStyle,
                            cell.Column.VisualCell,
                            DataGrid.VisualRowsFromStyle,
                            DataGrid.VisualRows);
                    }
                }
                // TRIGGER
                else if (enableTrigger != null)
                {
                    MergeVisual(cell.Label,
                        enableTrigger.VisualContainerStyle,
                        enableTrigger.VisualContainer,
                        cell.Column.VisualCellFromStyle,
                        cell.Column.VisualCell,
                        DataGrid.VisualRowsFromStyle,
                        DataGrid.VisualRows);
                }
                // DEFAULT
                else
                {
                    MergeVisual(cell.Label,
                        cell.Column.VisualCellFromStyle,
                        cell.Column.VisualCell,
                        DataGrid.VisualRowsFromStyle,
                        DataGrid.VisualRows);
                }
            }
        }

        internal void UpdateAutoNumeric(int num, int itemsCount)
        {
            // Auto numeric
            foreach(var cell in cells)
            {
                switch (cell.AutoNumber)
                {
                    case Enums.AutoNumberType.Up:
                        cell.Label.Text = (itemsCount + 1 - num).ToString(cell.Column.StringFormat);
                        break;
                    case Enums.AutoNumberType.Down:
                        cell.Label.Text = num.ToString(cell.Column.StringFormat);
                        break;
                }
            }
        }

        internal void UpdatePosition(int index)
        {
            Grid.SetRow(line, index);
            Grid.SetRow(touchContainer, index);
            Grid.SetRow(selectionContainer, index);

            foreach (var cell in cells)
                Grid.SetRow(cell.View, index);
        }

        private BoxView CreateHorizontalLine()
        {
            var line = new BoxView()
            {
                BackgroundColor = DataGrid.BorderColor,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = DataGrid.BorderWidth,
                InputTransparent = true,
            };
            return line;
        }

        private BoxView CreateSelectionContainer()
        {
            var line = new BoxView()
            {
                BackgroundColor = Color.Transparent,
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = 1.0,
                InputTransparent = true,
            };
            return line;
        }

        private void MergeVisual(Label label, params VisualCollector[] styles)
        {
            label.TextColor = ValueSelector.GetTextColor(styles);
            label.FontAttributes = ValueSelector.FontAttribute(styles);
            label.FontFamily = ValueSelector.FontFamily(styles);
            label.FontSize = ValueSelector.FontSize(styles);

            label.LineBreakMode = ValueSelector.GetLineBreakMode(styles);
            label.VerticalTextAlignment = ValueSelector.GetVerticalAlignment(styles);
            label.HorizontalTextAlignment = ValueSelector.GetHorizontalAlignment(styles);
        }
    }
}
