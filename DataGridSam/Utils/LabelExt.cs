using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    internal class LabelExt : Label
    {
        internal GridCell Cell;
        internal Row Row;
        public LabelExt(Row host, GridCell cell)
        {
            Row = host;
            Cell = cell;

            InputTransparent = true;
        }

        //protected override void OnParentSet()
        //{
        //    base.OnParentSet();

        //    Row.SolveSizeRow(Cell, Height);
        //}

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            //Row.SolveSizeRow(Cell, height);
        }
    }
}
