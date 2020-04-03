using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    internal class GridCell
    {
        internal readonly Row Row;
        internal Enums.AutoNumberType AutoNumber;
        internal bool IsCustomTemplate;
        internal bool IsSizeDone = true;
        internal View View;
        internal DataGridColumn Column;
        internal Label Label => View as Label;


        public GridCell(Row row, DataGridColumn column)
        {
            Row = row;
            Column = column;
        }

        internal void InitSizeChanged()
        {
            View.SizeChanged += OnCellSizeChanged;
        }

        internal void OnCellSizeChanged(object sender, EventArgs e)
        {
            if (IsSizeDone)
            {
                Row.SolveSizeRow(this, View.Height);
                IsSizeDone = false;
            }
        }
    }
}
