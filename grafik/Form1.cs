using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace grafik
{
    public partial class Form1 : Form
    {
        private Timer timer;
        private Random random;
        public Form1()
        {
            InitializeComponent();


            timer = new Timer();
            timer.Interval = 1000; // 1 saniyede bir çalışacak
            timer.Tick += Timer_Tick;

            // Random nesnesini oluştur
            random = new Random();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Timer durumunu tersine çevir (başlatılmışsa durdur, durdurulmuşsa başlat)
            if (timer.Enabled)
            {
                timer.Stop();
                button1.Text = "Başlat";
            }
            else
            {
                timer.Start();
                button1.Text = "Durdur";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            double xValue = chart1.Series[0].Points.Count;
            double yValue = random.NextDouble() * 100 * 100; // 0 ile 100 arasında rastgele bir sayı

            // Noktayı grafiğe ekle
            chart1.Series[0].Points.AddXY(xValue, yValue);

            // Grafikteki nokta sayısını sınırla (istediğiniz bir sayıya göre ayarlayabilirsiniz)
            if (chart1.Series[0].Points.Count > 10)
            {
                chart1.Series[0].Points.RemoveAt(0);
            }
        }
    }
}

