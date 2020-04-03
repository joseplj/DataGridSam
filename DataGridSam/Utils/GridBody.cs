using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    public class GridBody : Grid
    {
        internal bool HasItems;
        internal int ItemsCount;
        internal DataGrid DataGrid;
        internal Grid Mask;
        internal List<Row> Rows;

        public GridBody(DataGrid host)
        {
            // TODO
            BackgroundColor = Color.Red;
            DataGrid = host;
            Rows = new List<Row>();

            Mask = new Grid();
            Mask.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            Mask.ColumnSpacing = 0;
            Mask.BackgroundColor = Color.Transparent;
            Mask.InputTransparent = true;
            Children.Add(Mask);

            VerticalOptions = LayoutOptions.StartAndExpand;
            RowSpacing = 0;
            ColumnSpacing = 0;
        }

        // ItemsSource
        public static BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(GridBody), null, 
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: ItemsChanged);

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        internal void Init()
        {
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            // Init mask
            Mask.Children.Clear();
            Mask.ColumnDefinitions.Clear();

            if (DataGrid.Columns == null)
                return;

            // Create vertical borders (Table)
            int i = 0;
            foreach (var col in DataGrid.Columns)
            {
                ColumnDefinitions.Add(new ColumnDefinition { Width = col.CalcWidth });
                Mask.ColumnDefinitions.Add(new ColumnDefinition { Width = col.CalcWidth });

                if (i < DataGrid.Columns.Count - 1)
                {
                    var line = new BoxView
                    {
                        WidthRequest = DataGrid.BorderWidth,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.EndAndExpand,
                        BackgroundColor = DataGrid.BorderColor,
                        TranslationX = DataGrid.BorderWidth,
                    };
                    Grid.SetColumn(line, i);
                    Grid.SetRow(line, 0);
                    Mask.Children.Add(line);
                }

                i++;
            }
            //

            UpdateMask();
        }

        private static void ItemsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (GridBody)bindable;

            IEnumerable newList;
            try
            {
                newList = newValue as IEnumerable;
            }
            catch (Exception e)
            {
                self.ClearRows();
                throw e;
            }

            if (oldValue is INotifyCollectionChanged oldObservableCollection)
                oldObservableCollection.CollectionChanged -= self.OnItemsSourceCollectionChanged;

            if (newValue is INotifyCollectionChanged newObservableCollection)
                newObservableCollection.CollectionChanged += self.OnItemsSourceCollectionChanged;

            self.ClearRows();

            // Detect items count 
            self.ItemsCount = 0;
            if (newValue is ICollection collection)
            {
                self.ItemsCount = collection.Count;
            }
            else if (newList != null)
            {
                var enumerator = newList.GetEnumerator();
                if (enumerator != null)
                    while (enumerator.MoveNext())
                        self.ItemsCount++;
            }

            if (newList == null)
                return;

            // Create first rows
            int i = 0;
            Row lastRow = null;
            foreach (var item in newList)
            {
                // Say triggers what binding context changed
                if (i == 0)
                    self.DataGrid.OnChangeItemsBindingContext(item.GetType());

                lastRow = CreateRowAdd(item, self, i, self.ItemsCount);
                i++;
            }
            self.UpdateMask();

            // Hide last row line
            if (lastRow != null)
            {
                var row = (Row)lastRow;
                row.line.IsVisible = false;
            }

            self.HasItems = (self.ItemsCount > 0);
            //self.UpdateChildrenLayout();
            //self.InvalidateLayout();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                var del = Rows[e.OldStartingIndex];
                del.Remove();

                var item = e.NewItems[e.NewStartingIndex];
                var row = CreateRowInsert(item, this, e.NewStartingIndex, ItemsCount);

                // Hide line if row is last
                if (Rows.LastOrDefault() == row)
                    row.line.IsVisible = false;
            }
            // Add
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems == null)
                    return;

                // Common calc
                ItemsCount += e.NewItems.Count;

                bool isRowInsert = false;
                bool isRowAdded = false;

                if (Rows.Count == 0)
                    isRowAdded = true;
                else if (e.NewStartingIndex < Rows.Count)
                    isRowInsert = true;
                else
                    isRowAdded = true;

                Row row = null;

                // For add - last line set visible
                if (isRowAdded)
                {
                    row = Rows.LastOrDefault();
                    if (row != null)
                        row.line.IsVisible = true;
                }

                int pause = 0;
                for (var i = 0; i < e.NewItems.Count; ++i)
                {
                    int index = i + e.NewStartingIndex;
                    pause = index;
                    var item = e.NewItems[i];

                    if (isRowAdded)
                        row = CreateRowAdd(item, this, index, ItemsCount);
                    else if (isRowInsert)
                        row = CreateRowInsert(item, this, index, ItemsCount);

                    // line visibile
                    if (isRowAdded && index == Rows.Count - 1)
                        row.line.IsVisible = false;
                }

                // recalc autonumber
                if (DataGrid.IsAutoNumberCalc)
                {
                    if (DataGrid.AutoNumberStrategy == Enums.AutoNumberStrategyType.Both)
                    {
                        for (int i = 0; i < Rows.Count; i++)
                            Rows[i].UpdateAutoNumeric(i + 1, ItemsCount);
                    }
                    else if (DataGrid.AutoNumberStrategy == Enums.AutoNumberStrategyType.Down)
                    {
                        for (int i = pause; i < Rows.Count; i++)
                            Rows[i].UpdateAutoNumeric(i + 1, ItemsCount);
                    }
                    else if (DataGrid.AutoNumberStrategy == Enums.AutoNumberStrategyType.Up)
                    {
                        for (int i = pause; i >= 0; i--)
                            Rows[i].UpdateAutoNumeric(i + 1, ItemsCount);
                    }
                }

                // Redraw
                Redraw(e.NewStartingIndex);
            }
            // Remove
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems == null)
                    return;

                // Common calc
                ItemsCount -= e.OldItems.Count;

                
                int del = e.OldStartingIndex;
                bool isLast = (del == ItemsCount);
                var delRow = Rows[del];
                delRow.Remove();

                // hide last line
                if (isLast)
                {
                    var lastRow = Rows.LastOrDefault();
                    if (lastRow != null)
                    {
                        lastRow.line.IsVisible = false;
                    }
                }

                // recalc autonumber
                if (DataGrid.IsAutoNumberCalc)
                {
                    if (DataGrid.AutoNumberStrategy == Enums.AutoNumberStrategyType.Both)
                    {
                        for (int i = 0; i < Rows.Count; i++)
                            Rows[i].UpdateAutoNumeric(i + 1, ItemsCount);
                    }
                    else if (DataGrid.AutoNumberStrategy == Enums.AutoNumberStrategyType.Down)
                    {
                        for (int i = del; i < Rows.Count; i++)
                            Rows[i].UpdateAutoNumeric(i + 1, ItemsCount);
                    }
                    else if (DataGrid.AutoNumberStrategy == Enums.AutoNumberStrategyType.Up)
                    {
                        if (ItemsCount > 0)
                        {
                            del = del - 1;
                            if (del < 0)
                                del = 0;

                            for (int i = del; i >= 0; i--)
                                Rows[i].UpdateAutoNumeric(i + 1, ItemsCount);
                        }
                    }
                }

                // Redraw
                Redraw(del);
            }
            // Clear
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearRows();
            }
            else
            {
                return;
            }

            HasItems = (ItemsCount > 0);
        }

        private static Row CreateRowAdd(object bindItem, GridBody host, int index, int itemsCount)
        {
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var row = new Row(bindItem, host, index, itemsCount);
            host.Rows.Add(row);
            //host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });

            return row;
        }

        private static Row CreateRowInsert(object bindItem, GridBody host, int index, int itemsCount)
        {
            var row = new Row(bindItem, host, index, itemsCount);
            host.Rows.Insert(index, row);

            return row;
        }

        private void ClearRows()
        {
            foreach (var row in Rows)
                row.Remove();

            Rows.Clear();
            RowDefinitions.Clear();
            ItemsCount = 0;
            HasItems = false;
        }

        private void Redraw(int offset)
        {
            int i = 0;
            foreach (var row in Rows)
            {
                row.UpdatePosition(i);
                i++;
            }

            UpdateMask();
        }

        private void UpdateMask()
        {
            int rCount = Rows.Count;
            if (rCount == 0)
                rCount = 1;

            int cCount = DataGrid.Columns.Count;
            if (cCount == 0)
                cCount = 1;

            Grid.SetRowSpan(Mask, rCount);
            Grid.SetColumnSpan(Mask, cCount);
            RaiseChild(Mask);
        }
    }
}
