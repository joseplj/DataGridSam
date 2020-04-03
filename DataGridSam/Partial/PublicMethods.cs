using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DataGridSam
{
    public partial class DataGrid : Grid
    {
        public void ScrollToElement(int id, bool isAnimated)
        {
            if (id < 0)
                id = 0;
            else if (id > bodyGrid.Rows.Count - 1)
                id = bodyGrid.Rows.Count - 1;

            var element = bodyGrid.Rows[id];
            mainScroll.ScrollToAsync(element.touchContainer, ScrollToPosition.MakeVisible, isAnimated);
        }

        public async Task ScrollToElementAsync(int id, bool isAnimated)
        {
            if (id < 0)
                id = 0;
            else if (id > bodyGrid.Rows.Count - 1)
                id = bodyGrid.Rows.Count - 1;

            var element = bodyGrid.Rows[id];
            await mainScroll.ScrollToAsync(element.touchContainer, ScrollToPosition.MakeVisible, isAnimated);
        }
    }
}
