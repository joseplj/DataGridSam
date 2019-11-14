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
            stackList.Spacing = 0;
            stackList.ItemTemplate = new StackListTemplateSelector();

            verticalLines.SizeChanged += (obj, e) => 
            {
                CalcHeightColumnLines(stackList, verticalLines);
            };
        }

        // Columns
        public static readonly BindableProperty ColumnsProperty =
            BindableProperty.Create(nameof(Columns), typeof(ColumnCollection), typeof(DataGrid),
                propertyChanged: (bindableObj, o, n) =>
                {
                    (bindableObj as DataGrid).InitHeaderView();
                },
                defaultValueCreator: b =>
                {
                    return new ColumnCollection();
                });
        public ColumnCollection Columns
        {
            get { return (ColumnCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }


        // Items source
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(DataGrid), null,
                propertyChanged: (thisObject, oldValue, newValue) =>
                {
                    DataGrid self = thisObject as DataGrid;

                    self.stackList.ItemsSource = newValue as IEnumerable;
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

        // Selected item
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(DataGrid), null, BindingMode.TwoWay);
        public object SelectedItem 
        { 
            get { return GetValue(SelectedItemProperty); } 
            set { SetValue(SelectedItemProperty, value); } 
        }


        // Border width
        public static readonly BindableProperty BorderWidthProperty =
            BindableProperty.Create(nameof(BorderWidth), typeof(double), typeof(DataGrid), 2.0, BindingMode.Default);

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
        public Color HeaderBackgroundColor { 
            get { return (Color)GetValue(HeaderBackgroundColorProperty); }
            set { SetValue(HeaderBackgroundColorProperty, value); }
        }

        // Header text color
        public static readonly BindableProperty HeaderTextColorProperty =
            BindableProperty.Create(nameof(HeaderTextColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Black);
        public Color HeaderTextColor
        {
            get { return (Color)GetValue(HeaderTextColorProperty); }
            set { SetValue(HeaderTextColorProperty, value); }
        }

        // Header text size
        public static readonly BindableProperty HeaderFontSizeProperty =
            BindableProperty.Create(nameof(HeaderFontSize), typeof(double), typeof(DataGrid), defaultValue: 14.0);
        public double HeaderFontSize
        {
            get { return (double)GetValue(HeaderFontSizeProperty); }
            set { SetValue(HeaderFontSizeProperty, value); }
        }

        // Header label style
        public static readonly BindableProperty HeaderLabelStyleProperty =
            BindableProperty.Create(nameof(HeaderLabelStyle), typeof(Style), typeof(DataGrid));
        public Style HeaderLabelStyle
        {
            get { return (Style)GetValue(HeaderLabelStyleProperty); }
            set { SetValue(HeaderLabelStyleProperty, value); }
        }

        // Cell padding
        public static readonly BindableProperty CellPaddingProperty =
            BindableProperty.Create(nameof(CellPadding), typeof(Thickness), typeof(DataGrid), defaultValue: new Thickness(0,0,0,0),
                propertyChanged: (b, o, n)=> 
                {
                    var self = (DataGrid)b;
                    if (n is Thickness value)
                        self.UpdatePadding(value);
                });
        public Thickness CellPadding 
        { 
            get { return (Thickness)GetValue(CellPaddingProperty); }
            set { SetValue(CellPaddingProperty, value); }
        }

        // Selected row color
        public static readonly BindableProperty SelectedRowColorProperty =
            BindableProperty.Create(nameof(SelectedRowColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Beige);
        public Color SelectedRowColor
        {
            get { return (Color)GetValue(SelectedRowColorProperty); }
            set { SetValue(SelectedRowColorProperty, value); }
        }

        // Rows color
        public static readonly BindableProperty RowsColorProperty =
            BindableProperty.Create(nameof(RowsColor), typeof(Color), typeof(DataGrid), defaultValue: Color.White);
        public Color RowsColor
        {
            get { return (Color)GetValue(RowsColorProperty); }
            set { SetValue(RowsColorProperty, value); }
        }

        // Rows text size
        public static readonly BindableProperty RowsFontSizeProperty =
            BindableProperty.Create(nameof(RowsFontSize), typeof(double), typeof(DataGrid), defaultValue: 14.0);
        public double RowsFontSize
        {
            get { return (double)GetValue(RowsFontSizeProperty); }
            set { SetValue(RowsFontSizeProperty, value); }
        }
    }
}