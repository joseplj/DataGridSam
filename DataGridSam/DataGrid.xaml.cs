﻿using DataGridSam.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DataGridSam
{
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DataGrid : Grid
    {
        public DataGrid()
        {
            InitializeComponent();
            stackList.DataGrid = this;
            stackList.Spacing = 0;
            stackList.ItemTemplate = new StackListTemplateSelector();
        }

        // Columns
        public static readonly BindableProperty ColumnsProperty =
            BindableProperty.Create(nameof(Columns), typeof(List<DataGridColumn>), typeof(DataGrid),
                propertyChanged: (bindableObj, o, n) =>
                {
                    (bindableObj as DataGrid).InitHeaderView();
                },
                defaultValueCreator: b =>
                {
                    return new List<DataGridColumn>();
                });
        public List<DataGridColumn> Columns
        {
            get { return (List<DataGridColumn>)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        // Row triggers
        public static readonly BindableProperty RowTriggersProperty =
            BindableProperty.Create(nameof(RowTriggers), typeof(List<RowTrigger>), typeof(DataGrid), null,
                defaultValueCreator: b=>
                {
                    return new List<RowTrigger>();
                },
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    if (n == null)
                    {
                        self.RowTriggers.Clear();
                        return;
                    }
                });
        public List<RowTrigger> RowTriggers
        {
            get { return (List<RowTrigger>)GetValue(RowTriggersProperty); }
            set { SetValue(RowTriggersProperty, value); }
        }


        // Items source
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(DataGrid), null,
                propertyChanged: (thisObject, oldValue, newValue) =>
                {
                    DataGrid self = thisObject as DataGrid;

                    self.stackList.ItemsSource = newValue as ICollection;
                });
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Command selected item 
        public static readonly BindableProperty CommandSelectedItemProperty =
            BindableProperty.Create(nameof(CommandSelectedItem), typeof(ICommand), typeof(DataGrid), null);
        public ICommand CommandSelectedItem
        {
            get { return (ICommand)GetValue(CommandSelectedItemProperty); }
            set { SetValue(CommandSelectedItemProperty, value); }
        }

        // Command double click selected item 
        public static readonly BindableProperty CommandDoubleClickItemProperty =
            BindableProperty.Create(nameof(CommandDoubleClickItem), typeof(ICommand), typeof(DataGrid), null);
        public ICommand CommandDoubleClickItem
        {
            get { return (ICommand)GetValue(CommandDoubleClickItemProperty); }
            set { SetValue(CommandDoubleClickItemProperty, value); }
        }

        // Command double click interval item 
        public static readonly BindableProperty DoubleClickIntervalProperty =
            BindableProperty.Create(nameof(DoubleClickInterval), typeof(double), typeof(DataGrid), (double)300, 
                propertyChanged: (b, o, n) =>
                {
                    double value = (double)n;
                    if (value <= 0)
                        throw new Exception("Interval double click can not be less than zero or equal zero");
                });
        public double DoubleClickInterval
        {
            get { return (double)GetValue(DoubleClickIntervalProperty); }
            set { SetValue(DoubleClickIntervalProperty, value); }
        }

        // Selected item
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(DataGrid), null, BindingMode.TwoWay,
                propertyChanged: (b, o, newSelectedRow) =>
                {
                    var self = (DataGrid)b;

                    // no action, if we do internal work
                    if (self.blockThrowPropChanged)
                        return;

                    var lastRow = self.SelectedRow;

                    if (newSelectedRow == null && lastRow != null)
                    {
                        lastRow.isSelected = false;
                        lastRow.UpdateStyle();

                        self.SelectedRow = null;
                    }
                    else if (self.stackList.ItemsSource is IList list)
                    {
                        var match = list.IndexOf(newSelectedRow);

                        // Without pagination
                        if (self.PaginationItemCount == 0)
                        {
                            if (match >= 0 && self.stackList.Children.Count > 0)
                            {
                                var row = (Row)self.stackList.Children[match];
                                row.isSelected = true;
                                row.UpdateStyle();

                                self.SelectedRow = row;
                            }
                        }
                        // With pagination
                        else
                        {
                            int pageStart = self.PaginationCurrentPageStartIndex;
                            int dif = match - self.PaginationCurrentPageStartIndex;

                            if (dif >= 0 && dif <= pageStart+self.PaginationItemCount-1)
                            {
                                var row = (Row)self.stackList.Children[dif];
                                row.isSelected = true;
                                row.UpdateStyle();

                                self.SelectedRow = row;
                            }
                        }
                    }
                });
        public object SelectedItem 
        { 
            get { return GetValue(SelectedItemProperty); } 
            set { SetValue(SelectedItemProperty, value); } 
        }

        // Pagination items count
        public static readonly BindableProperty PaginationItemCountProperty =
            BindableProperty.Create(nameof(PaginationItemCount), typeof(int), typeof(DataGrid), 0);
        public int PaginationItemCount
        {
            get { return (int)GetValue(PaginationItemCountProperty); }
            set { SetValue(PaginationItemCountProperty, value); }
        }

        // Pagination current page
        public static readonly BindableProperty PaginationCurrentPageProperty =
            BindableProperty.Create(nameof(PaginationCurrentPage), typeof(int), typeof(DataGrid), 1);
        public int PaginationCurrentPage
        {
            get { return (int)GetValue(PaginationCurrentPageProperty); }
            set { SetValue(PaginationCurrentPageProperty, value); }
        }

        /// <summary>
        ///  Border width (default: 1)
        /// </summary>
        public static readonly BindableProperty BorderWidthProperty =
            BindableProperty.Create(nameof(BorderWidth), typeof(double), typeof(DataGrid), 1.0, BindingMode.Default);
        /// <summary>
        /// default: 2
        /// </summary>
        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }

        // Border color
        public static readonly BindableProperty BorderColorProperty =
            BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Gray);
        public Color BorderColor { 
            get { return (Color)GetValue(BorderColorProperty); } 
            set { SetValue(BorderColorProperty, value); } 
        }

        // Header height
        public static readonly BindableProperty HeaderHeightProperty =
            BindableProperty.Create(nameof(HeaderHeight), typeof(int), typeof(DataGrid), 0,
                propertyChanged: (b, o, n) => 
                {
                    var self = b as DataGrid;
                    var r = self.RowDefinitions.First();
                    int value = (int)n;

                    if (value == 0)
                        r.Height = GridLength.Auto;
                    else if (value > 0)
                        r.Height = new GridLength(value);
                });
        public int HeaderHeight
        {
            get { return (int)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); } 
        }

        // Header background color
        public static readonly BindableProperty HeaderBackgroundColorProperty =
            BindableProperty.Create(nameof(HeaderBackgroundColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Gray);
        /// <summary>
        /// Header background color. Default: Gray
        /// </summary>
        public Color HeaderBackgroundColor { 
            get { return (Color)GetValue(HeaderBackgroundColorProperty); }
            set { SetValue(HeaderBackgroundColorProperty, value); }
        }

        // Header text color
        public static readonly BindableProperty HeaderTextColorProperty =
            BindableProperty.Create(nameof(HeaderTextColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Black);
        /// <summary>
        /// Header text color. Default: Color.Black
        /// </summary>
        public Color HeaderTextColor
        {
            get { return (Color)GetValue(HeaderTextColorProperty); }
            set { SetValue(HeaderTextColorProperty, value); }
        }

        // Header font size
        public static readonly BindableProperty HeaderFontSizeProperty =
            BindableProperty.Create(nameof(HeaderFontSize), typeof(double), typeof(DataGrid), defaultValue: 14.0);
        /// <summary>
        /// Header font size. Default: 14
        /// </summary>
        public double HeaderFontSize
        {
            get { return (double)GetValue(HeaderFontSizeProperty); }
            set { SetValue(HeaderFontSizeProperty, value); }
        }

        // Header label style
        public static readonly BindableProperty HeaderLabelStyleProperty =
            BindableProperty.Create(nameof(HeaderLabelStyle), typeof(Style), typeof(DataGrid));
        /// <summary>
        /// Header label style. Default: null
        /// </summary>
        public Style HeaderLabelStyle
        {
            get { return (Style)GetValue(HeaderLabelStyleProperty); }
            set { SetValue(HeaderLabelStyleProperty, value); }
        }

        // Cell padding
        public static readonly BindableProperty CellPaddingProperty =
            BindableProperty.Create(nameof(CellPadding), typeof(Thickness), typeof(DataGrid), defaultValue: new Thickness(5));
        /// <summary>
        /// Cell padding (thickness). Default: {5,5,5,5}
        /// </summary>
        public Thickness CellPadding 
        { 
            get { return (Thickness)GetValue(CellPaddingProperty); }
            set { SetValue(CellPaddingProperty, value); }
        }

        // Selected row color
        public static readonly BindableProperty SelectedRowColorProperty =
            BindableProperty.Create(nameof(SelectedRowColor), typeof(Color), typeof(DataGrid), null);
        public Color SelectedRowColor
        {
            get { return (Color)GetValue(SelectedRowColorProperty); }
            set { SetValue(SelectedRowColorProperty, value); }
        }

        // Selected row text color
        public static readonly BindableProperty SelectedRowTextColorProperty =
            BindableProperty.Create(nameof(SelectedRowTextColor), typeof(Color), typeof(DataGrid), null);
        /// <summary>
        /// Selected row text color. Default: null
        /// </summary>
        public Color SelectedRowTextColor
        {
            get { return (Color)GetValue(SelectedRowTextColorProperty); }
            set { SetValue(SelectedRowTextColorProperty, value); }
        }

        // Selected row attribute
        public static readonly BindableProperty SelectedRowAttributeProperty =
            BindableProperty.Create(nameof(SelectedRowAttribute), typeof(FontAttributes), typeof(DataGrid), null);
        /// <summary>
        /// Selected row attribute. Default: null
        /// </summary>
        public FontAttributes SelectedRowAttribute
        {
            get { return (FontAttributes)GetValue(SelectedRowAttributeProperty); }
            set { SetValue(SelectedRowAttributeProperty, value); }
        }

        // Rows color (Background)
        public static readonly BindableProperty RowsColorProperty =
            BindableProperty.Create(nameof(RowsColor), typeof(Color), typeof(DataGrid), defaultValue: Color.White);
        /// <summary>
        /// Rows color (Background). Default: Color.White
        /// </summary>
        public Color RowsColor
        {
            get { return (Color)GetValue(RowsColorProperty); }
            set { SetValue(RowsColorProperty, value); }
        }

        //Rows text color
        public static readonly BindableProperty RowsTextColorProperty =
            BindableProperty.Create(nameof(RowsTextColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Black);
        /// <summary>
        /// Rows text color. Default: Color.Black
        /// </summary>
        public Color RowsTextColor
        {
            get { return (Color)GetValue(RowsTextColorProperty); }
            set { SetValue(RowsTextColorProperty, value); }
        }

        // Rows font size
        public static readonly BindableProperty RowsFontSizeProperty =
            BindableProperty.Create(nameof(RowsFontSize), typeof(double), typeof(DataGrid), defaultValue: 14.0);
        /// <summary>
        /// Rows font size. Default: 14
        /// </summary>
        public double RowsFontSize
        {
            get { return (double)GetValue(RowsFontSizeProperty); }
            set { SetValue(RowsFontSizeProperty, value); }
        }

        // Rows text attribute
        public static readonly BindableProperty RowsFontAttributeProperty =
            BindableProperty.Create(nameof(RowsFontAttribute), typeof(FontAttributes), typeof(DataGrid), null);
        /// <summary>
        /// Rows font attribute. Default: null
        /// </summary>
        public FontAttributes RowsFontAttribute
        {
            get { return (FontAttributes)GetValue(RowsFontAttributeProperty); }
            set { SetValue(RowsFontAttributeProperty, value); }
        }
    }
}