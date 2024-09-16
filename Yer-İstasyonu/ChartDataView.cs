using System.Drawing;
using System.Windows.Forms;

namespace Yer_İstasyonu
{
    public class ChartDataView
    {
        public Point? prevPosition { get; set; }
        public ToolTip tooltip { get; set; }

        public ChartDataView()
        {
            prevPosition = null;
            tooltip = new ToolTip();
        }
    }
}
