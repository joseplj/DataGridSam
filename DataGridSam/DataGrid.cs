﻿using DataGridSam.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace DataGridSam
{
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    [ContentProperty("Columns")]
    public partial class DataGrid : Grid
    {
        public static void Init() { }
        public DataGrid()
        {
            RowSpacing = 0;
            RowDefinitions.Add(new RowDefinition{ Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition{ Height = GridLength.Star });

            // Head Grid (1)
            headGrid = new Grid();
            headGrid.ColumnSpacing = 0;
            headGrid.RowSpacing = 0;
            headGrid.SetBinding(Grid.BackgroundColorProperty, new Binding(nameof(HeaderBackgroundColor), source: this));
            SetRow(headGrid, 0);
            Children.Add(headGrid);

            // Scroll (1)
            mainScroll = new ScrollView();
            SetRow(mainScroll, 1);
            Children.Add(mainScroll);

            // Body Grid (2)
            bodyGrid = new Grid();
            bodyGrid.VerticalOptions = LayoutOptions.Start;
            bodyGrid.RowSpacing = 0;
            bodyGrid.RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
            };
            mainScroll.Content = bodyGrid;

            // Stack list (3)
            stackList = new StackList();
            stackList.Spacing = 0;
            stackList.DataGrid = this;
            stackList.ItemTemplate = new StackListTemplateSelector();
            bodyGrid.Children.Add(stackList);


            // Mask Grid (3)
            maskGrid = new Grid();
            maskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            maskGrid.ColumnSpacing = 0;
            maskGrid.BackgroundColor = Color.Transparent;
            maskGrid.InputTransparent = true;
            maskGrid.SetBinding(Grid.IsVisibleProperty, new Binding(nameof(stackList.HasItems), source: stackList));

            bodyGrid.Children.Add(maskGrid);


            // Wrapper (1)
            wrapper = new BorderWrapper(this);
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
                defaultValueCreator: b =>
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

                    foreach (var item in self.RowTriggers)
                    {
                        item.OnAttached(self);
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
            BindableProperty.Create(nameof(CommandSelectedItem), typeof(ICommand), typeof(DataGrid), null,
                propertyChanged: (b, o, n)=>
                {
                    var self = b as DataGrid;
                    self.UpdateTapCommand(n as ICommand);
                });
        public ICommand CommandSelectedItem
        {
            get { return (ICommand)GetValue(CommandSelectedItemProperty); }
            set { SetValue(CommandSelectedItemProperty, value); }
        }



        // Command long tap item
        public static readonly BindableProperty CommandLongTapItemProperty =
            BindableProperty.Create(nameof(CommandLongTapItem), typeof(ICommand), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = b as DataGrid;
                    self.UpdateLongTapCommand(n as ICommand);
                });
        public ICommand CommandLongTapItem
        {
            get { return (ICommand)GetValue(CommandLongTapItemProperty); }
            set { SetValue(CommandLongTapItemProperty, value); }
        }




        // Selected item
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(DataGrid), null, BindingMode.TwoWay,
                propertyChanged: UpdateSelectedItem);
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
        /// <summary>
        /// Current page if pagination is enabled (default: 1)
        /// </summary>
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
        /// default: 1
        /// </summary>
        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }



        // Border color
        public static readonly BindableProperty BorderColorProperty =
            BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(DataGrid), Color.Gray);
        /// <summary>
        /// Border color (default: Color.Gray)
        /// </summary>
        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
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




        /// <summary>
        /// Color when user taped on item (Default: Accent)
        /// </summary>
        public static readonly BindableProperty TapColorProperty =
            BindableProperty.Create(nameof(TapColor), typeof(Color), typeof(DataGrid), Color.Accent);
        /// <summary>
        /// Color when user taped on item (Default: Accent)
        /// </summary>
        public Color TapColor
        {
            get { return (Color)GetValue(TapColorProperty); }
            set { SetValue(TapColorProperty, value); }
        }




        // Is wrapped by borders
        public static readonly BindableProperty IsWrappedProperty =
            BindableProperty.Create(nameof(IsWrapped), typeof(bool), typeof(DataGrid), true,
                propertyChanged: (b, o, n) =>
                {
                    (b as DataGrid).wrapper.Update();
                });
        /// <summary>
        /// Is wrapped by borders (default: true)
        /// </summary>
        public bool IsWrapped
        {
            get { return (bool)GetValue(IsWrappedProperty); }
            set { SetValue(IsWrappedProperty, value); }
        }



        // TODO DELETE?
        public static readonly BindableProperty IsAutoNumberProperty =
            BindableProperty.Create(nameof(IsAutoNumber), typeof(bool), typeof(DataGrid), false,
                propertyChanged: (b, o, n) =>
                {
                });
        public bool IsAutoNumber
        {
            get { return (bool)GetValue(IsAutoNumberProperty); }
            set { SetValue(IsAutoNumberProperty, value); }
        }

        #region header
        // Header has borders
        public static readonly BindableProperty HeaderHasBorderProperty =
            BindableProperty.Create(nameof(HeaderHasBorder), typeof(bool), typeof(DataGrid), true,
                propertyChanged: (b, o, n) =>
                {
                    var self = b as DataGrid;
                    self.InitHeaderView();
                });
        /// <summary>
        /// Default: true
        /// </summary>
        public bool HeaderHasBorder
        {
            get { return (bool)GetValue(HeaderHasBorderProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }




        // Header height
        public static readonly BindableProperty HeaderHeightProperty =
            BindableProperty.Create(nameof(HeaderHeight), typeof(int), typeof(DataGrid), 0,
                propertyChanged: (b, o, n) =>
                {
                    var self = b as DataGrid;
                    self.UpdateHeadHeight((int)n);
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
        public Color HeaderBackgroundColor
        {
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
            BindableProperty.Create(nameof(HeaderFontSize), typeof(double), typeof(DataGrid), defaultValue: 14.0,
                propertyChanged: (b,o,n)=>
                {
                    var self = (DataGrid)b;
                });
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
            BindableProperty.Create(nameof(HeaderLabelStyle), typeof(Style), typeof(DataGrid),
                propertyChanged: (b,o,n) =>
                {
                    var self = (DataGrid)b;
                    self.UpdateHeaderStyle(n as Style);
                });
        /// <summary>
        /// Header label style. Default: null
        /// </summary>
        public Style HeaderLabelStyle
        {
            get { return (Style)GetValue(HeaderLabelStyleProperty); }
            set { SetValue(HeaderLabelStyleProperty, value); }
        }
        #endregion

        #region selected row visual
        // Selected row text style
        public static readonly BindableProperty SelectedRowTextStyleProperty =
            BindableProperty.Create(nameof(SelectedRowTextStyle), typeof(Style), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    self.VisualSelectedRowFromStyle.OnUpdateStyle(n as Style);
                    self.SelectedRow?.UpdateStyle();
                });
        public Style SelectedRowTextStyle
        {
            get { return (Style)GetValue(SelectedRowTextStyleProperty); }
            set { SetValue(SelectedRowTextStyleProperty, value); }
        }


        // Selected row color
        public static readonly BindableProperty SelectedRowColorProperty =
            BindableProperty.Create(nameof(SelectedRowColor), typeof(Color), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    self.VisualSelectedRow.BackgroundColor = (Color)n;
                });
        public Color SelectedRowColor
        {
            get { return (Color)GetValue(SelectedRowColorProperty); }
            set { SetValue(SelectedRowColorProperty, value); }
        }



        // Selected row text color
        public static readonly BindableProperty SelectedRowTextColorProperty =
            BindableProperty.Create(nameof(SelectedRowTextColor), typeof(Color), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    self.VisualSelectedRow.TextColor = (Color)n;
                });
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
            BindableProperty.Create(nameof(SelectedRowAttribute), typeof(FontAttributes), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    self.VisualSelectedRow.FontAttribute = (FontAttributes)n;
                });
        /// <summary>
        /// Selected row attribute. Default: null
        /// </summary>
        public FontAttributes SelectedRowAttribute
        {
            get { return (FontAttributes)GetValue(SelectedRowAttributeProperty); }
            set { SetValue(SelectedRowAttributeProperty, value); }
        }




        // Selected row font family
        public static readonly BindableProperty SelectedRowFontFamilyProperty =
            BindableProperty.Create(nameof(SelectedRowFontFamily), typeof(string), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    self.VisualSelectedRow.FontFamily = (string)n;
                });
        /// <summary>
        /// Default: null
        /// </summary>
        public string SelectedRowFontFamily
        {
            get { return (string)GetValue(SelectedRowFontFamilyProperty); }
            set { SetValue(SelectedRowFontFamilyProperty, value); }
        }
        #endregion

        #region row visual
        // Rows background color
        public static readonly BindableProperty RowsColorProperty =
            BindableProperty.Create(nameof(RowsColor), typeof(Color), typeof(DataGrid), defaultValue: Color.White,
                propertyChanged:(b,o,n)=>
                {
                    var self = (DataGrid)b;
                    self.VisualRows.BackgroundColor = (Color)n;
                });
        /// <summary>
        /// Rows background color. Default: Color.White
        /// </summary>
        public Color RowsColor
        {
            get { return (Color)GetValue(RowsColorProperty); }
            set { SetValue(RowsColorProperty, value); }
        }





        //Rows text style
        public static readonly BindableProperty RowsTextStyleProperty =
            BindableProperty.Create(nameof(RowsTextStyle), typeof(Style), typeof(DataGrid), null,
                propertyChanged:(b,o,n)=>
                {
                    var self = (DataGrid)b;
                    self.VisualRowsFromStyle.OnUpdateStyle(n as Style);
                });
        /// <summary>
        /// Rows text style. Default: null
        /// </summary>
        public Style RowsTextStyle
        {
            get { return (Style)GetValue(RowsTextStyleProperty); }
            set { SetValue(RowsTextStyleProperty, value); }
        }




        //Rows text color
        public static readonly BindableProperty RowsTextColorProperty =
            BindableProperty.Create(nameof(RowsTextColor), typeof(Color), typeof(DataGrid), defaultValue: Color.Black,
                propertyChanged: (b,o,n)=>
                {
                    var self = (DataGrid)b;
                    self.VisualRows.TextColor = (Color)n;
                });
        /// <summary>
        /// Rows text color. Default: Color.Black
        /// </summary>
        public Color RowsTextColor
        {
            get { return (Color)GetValue(RowsTextColorProperty); }
            set { SetValue(RowsTextColorProperty, value); }
        }


        // Rows font family
        public static readonly BindableProperty RowsFontFamilyProperty =
            BindableProperty.Create(nameof(RowsFontFamily), typeof(string), typeof(DataGrid), null,
                propertyChanged: (b, o, n) =>
                {
                    var self = (DataGrid)b;
                    self.VisualRows.FontFamily = (string)n;
                });
        /// <summary>
        /// Default: null
        /// </summary>
        public string RowsFontFamily
        {
            get { return (string)GetValue(RowsFontFamilyProperty); }
            set { SetValue(RowsFontFamilyProperty, value); }
        }


        // Rows font size
        public static readonly BindableProperty RowsFontSizeProperty =
            BindableProperty.Create(nameof(RowsFontSize), typeof(double), typeof(DataGrid), defaultValue: 14.0,
                propertyChanged:(b,o,n)=>
                {
                    var self = (DataGrid)b;
                    self.VisualRows.FontSize = (double)n;
                });
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
            BindableProperty.Create(nameof(RowsFontAttribute), typeof(FontAttributes), typeof(DataGrid), null,
                propertyChanged:(b,o,n)=>
                {
                    var self = (DataGrid)b;
                    self.VisualRows.FontAttribute = (FontAttributes)n;
                });
        /// <summary>
        /// Rows font attribute. Default: null
        /// </summary>
        public FontAttributes RowsFontAttribute
        {
            get { return (FontAttributes)GetValue(RowsFontAttributeProperty); }
            set { SetValue(RowsFontAttributeProperty, value); }
        }
        #endregion
    }
}
