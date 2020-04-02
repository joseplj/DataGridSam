using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    internal class GridBody : Grid
    {
        internal bool HasItems;
        internal int ItemsCount;
        internal DataGrid DataGrid;
        internal List<Row> Rows;

        public GridBody(DataGrid host)
        {
            DataGrid = host;
            Rows = new List<Row>();
            VerticalOptions = LayoutOptions.Start;
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
                Rows.Remove(del);
                Children.Remove(del);

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
                Rows.Remove(delRow);
                Children.Remove(delRow);

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
            var row = new Row(bindItem, host.DataGrid, index, itemsCount);

            Grid.SetRow(row, index);
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            host.Children.Add(row);
            host.Rows.Add(row);

            return row;
        }

        private static Row CreateRowInsert(object bindItem, GridBody host, int index, int itemsCount)
        {
            var row = new Row(bindItem, host.DataGrid, index, itemsCount);

            Grid.SetRow(row, index);
            host.Children.Add(row);
            host.Rows.Insert(index, row);

            return row;
        }

        private void ClearRows()
        {
            foreach (var row in Rows)
                Children.Remove(row);

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
                Grid.SetRow(row, i);
                i++;
            }

            UpdateMask();
        }

        private void UpdateMask()
        {
            var mask = DataGrid.maskGrid;
            Grid.SetRowSpan(mask, Rows.Count);
            RaiseChild(mask);
        }
    }
}
