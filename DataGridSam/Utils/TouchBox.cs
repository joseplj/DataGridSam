using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    public class TouchBox : BoxView
    {
        public readonly Row Row;
        public TouchBox(Row row)
        {
            Row = row;
        }
    }
}
