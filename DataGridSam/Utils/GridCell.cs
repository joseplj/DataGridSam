﻿using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DataGridSam.Utils
{
    internal class GridCell
    {
        internal Enums.AutoNumberType AutoNumber;
        internal bool IsCustomTemplate;
        internal ContentView Wrapper;
        internal Label Label;
        internal DataGridColumn Column;
    }
}
