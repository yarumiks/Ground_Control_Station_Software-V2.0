using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using Yer_İstasyonu.STL_Tools;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Globalization;
using System.Threading.Tasks;
using Label = System.Windows.Forms.Label;
using System.IO.Ports;
using System.Collections.Concurrent;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Controls;
using Image = System.Drawing.Image;
using System.Collections.Generic;
using System.Reflection.Emit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;     
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using System.Windows.Markup;
using System.IO.Packaging;
using System.Reflection;

namespace Yer_İstasyonu
{
    public partial class Form1 : Form
    {

        #region DEĞİŞKENLER
        public List<Label> rocketList;
        public List<Label> dutyLoadList;
        string[] split;
        byte[] package = new byte[78];
        string[] splitPayload;
        double time;
        int counter = 0;
        //GPS ve Kamera İçin Değişkenler
        private Camera_Module cm;
        private GMapOverlay markersOverlay;
        private GMapOverlay markersOverlay2;
        public GMapOverlay MarkerOverlar { get { return markersOverlay; } set { markersOverlay = value; } }
        public GMapOverlay MarkerOverlar2 { get { return markersOverlay2; } set { markersOverlay2 = value; } }

        
        //butonların tıklanma durumlarına göre statelerin değişmesi için kullanılacak referans
        //seri iletişim için tıklanan butonlarda kullanılıyor.
        public bool aviyonicsClick, dutyLoadClick, RefereeClick,clearClick, sendDataState, saveVideoClick;
        private string aviyonicsLn, dutyLoadLn, refereeLn; //Gelen tek satırlık verileri karşılar.

        //buffer kullanarak anlık donmaların önüne geçilmeye çalışıldı.
        private ConcurrentQueue<string> dataBuffer = new ConcurrentQueue<string>();
        private int maxBufferSize = 100; // Max buffer size
        private double e = 2.71;
        private bool isRecording = false;
        //Grafik değişkenleri
        private ChartDataView[] chartDataViews = new ChartDataView[4]
        {
            new ChartDataView(),
            new ChartDataView(),
            new ChartDataView(),
            new ChartDataView(), 
        };

        #endregion
        public Form1()
        {
            InitializeComponent();
            InitializeGPS();

            // Aviyonik Bilgileri gösteren labelların listesi
            rocketList = new List<Label> {
                label22, label23, //Enlem, Boylam //GPS
                label24, label25, label26, //basınç,sıcaklık,alttitude //M
                label27, label28, label29, //gyro x,y,z 
                label30, label31, label32, //accelx,y,z açı //MP

                //label30, label31, label32, //accelx,y,z açı //MP
                //label27, label28, label29, //gyro x,y,z 

                //label33, label34, label35,
                //G,38.667782,34.737743
                //MS,32.7861,877.4431,1304.6714
                //MP,0.14,0.00,-0.93
               
        };

            // Görev Yükü Bilgileri gösteren labelların listesi (Enlem,Boylam, gpsYükseklik, Sıcaklık,Basınç, İrtifa,Nem, Gerinim, gpsYükseklik)
            dutyLoadList = new List<Label> { label36, label37, label39, label42, label38, label40,
            //label16
            };
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            await Task.Delay(1000);
            BatuGL.Configure(glControl1, BatuGL.Ortho_Mode.CENTER);
            FileMenuImportBt_Click(this, EventArgs.Empty);
            cm = new Camera_Module();
            cm.AddCombobox(comboBox3);
        }




        #region BAĞLANTI AYARLARI
        private void GetPorts()
        {
            comboBox1.Items.Clear();
            string[] ports = SerialPort.GetPortNames();

            try
            {
                if (!comboBox1.Items.Contains(ports))
                    comboBox1.Items.AddRange(ports);
            }
            catch (Exception hata)
            {
                MessageBox.Show($"Portlar Listelenirken Hata Oluştu\nHata: {hata.Message}");
            }

            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;
        }

        private void Refresh_Click(object sender, EventArgs e) => GetPorts();


        private void ClearData_Click(object sender, EventArgs e)
        {
            foreach (Label label in rocketList)
            {
                label.Text = "0";
            }
            foreach (Label label in dutyLoadList)
            {
                label.Text = "0";
            }


            Chart[] charts = { chart1, chart2, chart3, chart4 };

            foreach (var chart in charts)
            {
                chart.Series[0].Points.Clear();
                chart.Series[0].Points.AddXY(0, 0);
                chart.ChartAreas[0].AxisX.Minimum = double.NaN;
                chart.ChartAreas[0].AxisX.Maximum = double.NaN;
                chart.ChartAreas[0].AxisY.Minimum = double.NaN;
                chart.ChartAreas[0].AxisY.Maximum = double.NaN;
            }
            clearClick = true;
            glControl1.Invalidate();
        }

        private void avionicsBtn_Click(object sender, EventArgs e)
        {

            if (!aviyonicsClick && !Aviyonik.IsOpen)
            {
                try
                {
                    if (comboBox1.SelectedItem == null || comboBox2.SelectedItem == null)
                    {
                        MessageBox.Show("Bir port ve baud rate seçiniz.", "Port Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    Aviyonik.PortName = comboBox1.SelectedItem.ToString();
                    Aviyonik.BaudRate = Convert.ToInt32(comboBox2.SelectedItem); //9600
                    Aviyonik.Parity =  Parity.None;
                    Aviyonik.StopBits = StopBits.One;
                    Aviyonik.DataBits = 8;
                    Aviyonik.Handshake = Handshake.None;
                    Aviyonik.Open();
                    Aviyonik.DataReceived +=Aviyonik_DataReceived;

                    avionicsBtn.Text= "Bağlantı Kes";
                    avionicsBtn.BackColor = Color.DarkSlateBlue;
                    comboBox1.Text = "";
                    comboBox2.Text = "";
                }
                catch (Exception err)
                {
                    MessageBox.Show($"Bağlanma Hatası: {err.Message}");
                    throw;
                }
                aviyonicsClick = true;
            }
            else
            {
                try
                {
                    Aviyonik.DataReceived -= Aviyonik_DataReceived;
                    Aviyonik.DiscardInBuffer();
                    dataBuffer = new ConcurrentQueue<string>();
                    Aviyonik.Close();
                    avionicsBtn.Text = "Aviyonik Bağlantısı";
                    avionicsBtn.BackColor = default;

                }
                catch (Exception err)
                {
                    MessageBox.Show($"Sistem kapatılırken bir Hata Oluştu: {err.Message}");
                }
                aviyonicsClick = false;
            }
        }


        private void payloadBtn_Click(object sender, EventArgs e)
        {
            if (!dutyLoadClick && !Yük.IsOpen)
            {
                try
                {
                    if (comboBox1.SelectedItem == null || comboBox2.SelectedItem == null)
                    {
                        MessageBox.Show("Bir port ve baud rate seçiniz.", "Port Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    Yük.PortName = comboBox1.SelectedItem.ToString();
                    Yük.BaudRate =  Convert.ToInt32(comboBox2.SelectedItem);
                    Yük.Parity =  Parity.None;
                    Yük.StopBits = StopBits.One;
                    Yük.DataBits = 8;
                    Yük.Handshake = Handshake.None;
                    Yük.Open();
                    Yük.DataReceived +=Yük_DataReceived;

                    payloadBtn.Text= "Bağlantı Kes";
                    payloadBtn.BackColor = Color.DarkSlateBlue;
                    comboBox1.Text = "";
                    comboBox2.Text = "";

                }
                catch (Exception err)
                {
                    MessageBox.Show($"Bağlanma Hatası: {err.Message}");
                    throw;
                }
                dutyLoadClick = true;
            }
            else
            {
                try
                {
                    Yük.DataReceived -= Yük_DataReceived;
                    Yük.DiscardInBuffer();
                    dataBuffer = new ConcurrentQueue<string>();
                    Yük.Close();
                    payloadBtn.Text = "Görev Yükü Bağlantısı";
                    payloadBtn.BackColor = default;

                }
                catch (Exception err)
                {
                    MessageBox.Show($"Sistem kapatılırken bir Hata Oluştu: {err.Message}");
                }
                dutyLoadClick = false;
            }
        }
        #endregion
        #region VERİ YAZDIRMA
        #region Aviyonik
        private async void Aviyonik_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (Aviyonik.IsOpen)
                {
                    aviyonicsLn = Aviyonik.ReadLine();
                    //dataBuffer.Enqueue(aviyonicsLn);

                    //if (dataBuffer.Count > maxBufferSize)
                    //{
                    //    string removedItem;
                    //    while (dataBuffer.TryDequeue(out removedItem)) ;
                    //}
                    //Seri port açıldığında bu işlemi asenkron olarak gerçekleştirmesini sağlıyor
                    await ProcessAviyonicsDataAsync();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"Port Sonlandırma Hatası: ${err.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        //İşlemleri arka planda Thread açarak arayüzün kasmasını engelleniyor
        private async Task ProcessAviyonicsDataAsync()
        {
            await Task.Run(() =>
            {
                AviyonicsReceiveData();
            });
        }


        //Bütün veri yazdırma işlemleri burada gerçekleştiriyor
        //Bunları tek seferde gerçekleştiriyorum(grafik güncelleme, eksen) istenirse her bir güncellemeyi başka fonksiyonlara ekleyerek asenkron olarak bu kodun içine eklenebilir daha dinamik olmassı açısından
        private void AviyonicsReceiveData()
        {
           

              split = aviyonicsLn.Split('*');

            if (split != null && split.Length > 3)
            {
                //Tek satırlık veri paketi için
                try
                {
                    for (int i = 0; i < rocketList.Count && i < split.Length; i++) /*&& /*i < dataGridView1.Columns.Count; i++*/
                    {
                        if (rocketList[i].InvokeRequired)
                        {
                            if (split.Length > i)
                            {
                                rocketList[i].Invoke(new MethodInvoker(delegate { rocketList[i].Text = split[i].ToString(); }));
                            }
                        }
                        else
                        {
                            rocketList[i].Text = "0";
                        }
                    }

                    //------------HARİTALAR----------------// 

                    this.Invoke(new Action(TemperatureGrapth));
                    this.Invoke(new Action(PressureGrapth));
                    this.Invoke(new Action(AltitudeGrapth));
                    this.Invoke(new Action(GyroGrapth));
                    //------------HARİTALAR SON ----------------// 



                    //--------------- EKSEN ---------------//
                   // Veriler tüm paket olarak geliyorsa
                    if (split.Length >= 5)
                    {
                        Invoke((MethodInvoker)delegate
                        {

                            if (double.TryParse(split[5], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedX) &&
                                double.TryParse(split[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedY) &&
                                double.TryParse(split[10], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedZ))
                            {
                                // Tüm dönüştürmeler başarılı olduğunda dönüştürülen değerler eksenlere atanır.
                                //x = parsedX * 33;
                                //y = parsedY * 33;
                                z = parsedZ * 75;
                            }
                            glControl1.Invoke(new Action(() => glControl1.Invalidate()));
                        });
                    }
                    //--------------- EKSEN SON ---------------//


                    //--------------- GPS ---------------//
                    //Aviyonik Konum

                    this.Invoke(new Action(() =>
                     {
                         DisplayGPS(gMapControl1, markersOverlay, label22.Text, label23.Text);
                     }));

                    //--------------- GPS SON ---------------//

                }
                catch (Exception err)
                {
                    
                }
            }
            else
            {
                Console.WriteLine($"Aviyonik verilerinde index uyuşmazlığı var");
            }
        }
        #endregion
        #region Görev Yük

        private async void Yük_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (Yük.IsOpen)
                {
                    dutyLoadLn = Yük.ReadLine();
                    //dataBuffer.Enqueue(aviyonicsLn);

                    //if (dataBuffer.Count > maxBufferSize)
                    //{
                    //    string removedItem;
                    //    while (dataBuffer.TryDequeue(out removedItem)) ;
                    //}

                    //Seri port açıldığında bu işlemi asenkron olarak gerçekleştirmesini sağlıyor
                    await ProcessDutyPayloadDataAsync();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"Port Sonlandırma Hatası: ${err.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ProcessDutyPayloadDataAsync()
        {
            await Task.Run(() =>
            {
                DutyPayloadReceiveData();
            });
        }

        private void DutyPayloadReceiveData()
        {
            try
            {
                splitPayload = dutyLoadLn.Split('*');

                if (splitPayload != null && splitPayload.Length > 0)
                {

                    for (int i = 0; i < dutyLoadList.Count && i < splitPayload.Length; i++) 
                    {
                        if (dutyLoadList[i].InvokeRequired)
                        {
                            if (splitPayload.Length > i)
                            {
                                dutyLoadList[i].Invoke(new MethodInvoker(delegate { dutyLoadList[i].Text = splitPayload[i].ToString(); }));
                            }
                        }
                        else
                        {
                            dutyLoadList[i].Text = "0";
                        }

                        //this.Invoke(new Action(StrainGaugeGrapth));
                    }

                    //Görev yükü Konum bilgisi 
                    this.Invoke(new Action(() =>
                    {
                        DisplayGPS(gMapControl2, markersOverlay2, label36.Text, label37.Text);
                    }));

                }
            }
            catch (Exception err) {
               
            }
        }
        #endregion
        #region Hakem
        private void refereeBtn_Click(object sender, EventArgs e)
        {
            if (!RefereeClick && !Hakem.IsOpen)
            {
                try
                {
                    Hakem.PortName = comboBox1.SelectedItem.ToString();
                    Hakem.BaudRate = 19200; //Convert.toint32(combobo2.Text);
                    Hakem.Parity = Parity.None;
                    Hakem.StopBits = StopBits.One;
                    Hakem.DataBits = 8;
                    Hakem.Handshake = Handshake.None;
                    Hakem.Open();

                    refereeBtn.Text= "\tBağlantı Kes";
                    refereeBtn.BackColor = Color.DarkSlateBlue;
                    comboBox1.Text = "";
                    comboBox2.Text = "";
                }
                catch (Exception err)
                {
                    MessageBox.Show($"Bağlanma Hatası: {err.Message}");
                    throw;
                }
                RefereeClick = true;
            }
            else
            {
                try
                {
                    Hakem.Close();
                    refereeBtn.Text = "Hakem Bağlantı";
                    refereeBtn.BackColor = Color.Transparent;
                }
                catch (Exception err)
                {
                    MessageBox.Show($"Sistem kapatılırken bir Hata Oluştu: {err.Message}");
                }
                RefereeClick = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            SendPackage();
        }

        #endregion
        #endregion
        #region Hakem Veri Gönderme İşlemleri
        class FLOAT_DONUSTURUCU
        {
            public float number;
            public byte[] array;
            public void convert() => array = BitConverter.GetBytes(number);
        }


        private byte CheckSum()
        {
            int sum = 0;
            for (int i = 4; i < 75; i++)
            {
                sum += package[i];
            }
            return (byte)(sum % 256);
        }


        private void CreatePackage()
        {
            package[0] = 0xFF;
            package[1] = 0xFF;
            package[2] = 0x54; //T
            package[3] = 0x52; //R

            package[4] = 207; //takim ID
            package[5] = (byte)counter; //sayac
            
            //dutyLoadList, rocketList verilerini sırayla doldurulacak!!!
            float[] values = new float[]
          {
            float.TryParse(label26.Text,out float irtifa) ? irtifa : 0f,  // İrtifa
            float.TryParse(label39.Text,out float gpsirtifa) ? gpsirtifa : 0f,  // GPS İrtifa
            float.TryParse(label22.Text,out float gpsenlem) ? gpsenlem : 0f,  // Roket Enlem
            float.TryParse(label23.Text,out float gpsboylam) ? gpsboylam : 0f, // Roket Boylam
            float.TryParse(label39.Text,out float gygpsirt) ? gygpsirt : 0f, // Görev Yükü GPS İrtifa 
            float.TryParse(label36.Text,out float gyenlem) ? gyenlem : 0f, // Görev Yükü Enlem 
            float.TryParse(label37.Text,out float gyboylam) ? gyboylam : 0f, // Görev Yükü Boylam 
            0f, // Kademe GPS İrtifa
            0f, // Kademe Enlem
            0f, // Kademe Boylam
            float.TryParse(label27.Text,out float gyroX) ? gyroX : 0f, // Jiroskop X 
            float.TryParse(label28.Text,out float gyroY) ? gyroY : 0f, // Jiroskop Y
            float.TryParse(label29.Text,out float gyroZ) ? gyroZ : 0f, // Jiroskop Z
            float.TryParse(label30.Text,out float accX) ? gyroZ : 0f, // İvme X 
            float.TryParse(label31.Text,out float accY) ? gyroZ : 0f, // İvme Y
            float.TryParse(label32.Text,out float accZ) ? gyroZ : 0f, // İvme Z 
          };

            // Verileri paket dizisine ekle
            int offset = 6;
            foreach (var value in values)
            {
                FLOAT_DONUSTURUCU converter = new FLOAT_DONUSTURUCU { number = value };
                converter.convert();
                Array.Copy(converter.array, 0, package, offset, 4);
                offset += 4;
            }

            package[75] = CheckSum();

            package[76] = 0x0D;
            package[77] = 0x0A;
        }


        private void SendPackage()
        {
            ++counter;
            CreatePackage();
            Hakem.Write(package, 0, package.Length);
        }
        #endregion

        #region KAMERA

        private void OpenCamera_Click(object sender, EventArgs e)
        {
            if (pictureBox1 != null && comboBox3.SelectedIndex != -1)
            {
                cm.Connect(comboBox3.SelectedIndex, pictureBox1);
            }
            else
            {
                MessageBox.Show("PictureBox kontrolü formda eklenmemiş.");
            }
        }


        private void SaveVideo_Click(object sender, EventArgs e)
        {

            if (cm.IsRecording) // Eğer kayıt başlamışsa
            {
                cm.StopRecording();
                cm.Disconnect(pictureBox1);
                saveVideoClick = false;
                startRecord.Text = "Start Recording";
            }
            else
            {   
                cm.Disconnect(pictureBox1);
            }
        }

        private void startRecord_Click(object sender, EventArgs e)
        {
               if (comboBox3.SelectedIndex != -1)
              {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string outputPath = Path.Combine(desktopPath, "Video1.mkv");

                cm.Connect(comboBox3.SelectedIndex, pictureBox1);
                cm.StartRecording();
                saveVideoClick = true;

                if (saveVideoClick) startRecord.Text = "Recording";
               
              }
            else
            {
                MessageBox.Show("Lütfen bir kamera seçin.");
            }
        }
        #endregion
        #region GRAFİKLER

        //Verilen kordinatların karışmaması için ayrı eventleri oluşturuyoruz.
        private void Chart1_MouseMove(object sender, MouseEventArgs e)
            => Chart_MouseMove(chartDataViews[0], e, chart1);
        private void Chart2_MouseMove(object sender, MouseEventArgs e)
            => Chart_MouseMove(chartDataViews[1], e, chart2);
        private void Chart3_MouseMove(object sender, MouseEventArgs e)
            => Chart_MouseMove(chartDataViews[2], e, chart3);
        private void Chart4_MouseMove(object sender, MouseEventArgs e)
            => Chart_MouseMove(chartDataViews[3], e, chart4);


        //Mouse hareketi ile X ve Y kordinat değerlerini göstermemize sağlayan ana fonksiyonumuz
        private void Chart_MouseMove(ChartDataView chartData, MouseEventArgs e, Chart chart)
        {
            var pos = e.Location;

            if (chartData == null || chartData.prevPosition.HasValue && pos == chartData.prevPosition.Value)
                return;

            chartData.tooltip.RemoveAll();
            chartData.prevPosition = pos;

            var results = chart.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.ChartElementType == ChartElementType.DataPoint)
                    {
                        var prop = result.Object as DataPoint;
                        if (prop != null)
                        {
                            var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                            var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                            if (Math.Abs(pos.X - pointXPixel) < 5 && Math.Abs(pos.Y - pointYPixel) < 5)
                            {
                                chartData.tooltip.Show("X= " + prop.XValue + ", Y= " + prop.YValues[0], chart, pos.X, pos.Y - 15);
                            }
                        }
                    }
                }
            }
        } 

        private void TemperatureGrapth()
        {
            var tempChart = chart1.ChartAreas[0];
           
            if (aviyonicsLn != null && split.Length >= 3) 
            {
            chart1.Series[0].Points.AddXY(time, split[3]);
                if (chart1.Series[0].Points.Count >100) chart1.Series[0].Points.RemoveAt(0);

                time += 1.0; //x ekseni için double türünde zaman değişkeni
                tempChart.AxisX.Minimum =chart1.Series[0].Points[0].XValue;
                tempChart.AxisX.Maximum =time;

                double minY = 0; // Minimum değer
                double maxY = 100;  // Maksimum değer

                tempChart.AxisY.Minimum = minY;
                tempChart.AxisY.Maximum = maxY;
                tempChart.AxisY.IsStartedFromZero = false;

                tempChart.AxisY.Interval = 15; // Aralık değer
                tempChart.AxisY.IntervalOffset = minY % 10; // Aralık ofseti
                tempChart.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number; // Aralık tipi

                tempChart.AxisX.LabelStyle.Format = "0.0";

                //buradaki koşullar Y ekseninde max ve min değerlerini verdikten sonra
                //eğer o aralık dışında kalan bir değerse aralıkları güncelliyor +100 veya -100 şeklinde
                if (chart1.Series[0].Points.Any())
                {
                    //sabit olması için int olarak Cast ediyoruz
                    double currentMaxY = (int)chart1.Series[0].Points.Max(p => p.YValues[0]);
                    double currentMinY = (int)chart1.Series[0].Points.Min(p => p.YValues[0]);

                    if (currentMaxY > maxY)
                    {
                        tempChart.AxisY.Maximum = currentMaxY + 100;
                        //tempChart.AxisY.Minimum = currentMinY + 50;
                    }

                    if (currentMinY < minY)
                    {
                        tempChart.AxisY.Minimum = currentMinY - 100;
                       // tempChart.AxisY.Maximum = currentMaxY - 50;
                    }
                }
                else
                {
                    Console.WriteLine("Veri ayrıştırılırken bir hata oluştu.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PressureGrapth()
        {
            var pressurechart = chart4.ChartAreas[0];
            if (aviyonicsLn != null && split.Length >= 3)
            {
                chart4.Series[0].Points.AddXY(time, split[2]);
                if (chart4.Series[0].Points.Count >100) chart4.Series[0].Points.RemoveAt(0);

                time += 1.0;
                pressurechart.AxisX.Minimum =chart4.Series[0].Points[0].XValue;
                pressurechart.AxisX.Maximum =time;

                double minY = -200;
                double maxY = 10000;

                pressurechart.AxisY.Minimum = minY;
                pressurechart.AxisY.Maximum = maxY;
                pressurechart.AxisY.IsStartedFromZero = false;

                pressurechart.AxisY.Interval = 10000;
                pressurechart.AxisY.IntervalOffset = minY % 80;
                pressurechart.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;

                pressurechart.AxisX.LabelStyle.Format = "0.0";

                if (chart4.Series[0].Points.Count > 0)
                {
                    //sabit olması için int olarak Cast e3diyoruz
                    double currentMaxY = (int)chart4.Series[0].Points.Max(p => p.YValues[0]);
                    double currentMinY = (int)chart4.Series[0].Points.Min(p => p.YValues[0]);

                    if (currentMaxY > maxY)
                    {
                        pressurechart.AxisY.Maximum = currentMaxY + 500;
                        //pressurechart.AxisY.Minimum = currentMinY + 250;
                    }

                    if (currentMinY < minY)
                    {
                        pressurechart.AxisY.Minimum = currentMinY - 500;
                        //pressurechart.AxisY.Maximum = currentMaxY - 250;
                    }
                }
            }
        }


        private void AltitudeGrapth()
        {
            var altitudeChart = chart2.ChartAreas[0];
            if (aviyonicsLn != null && split.Length >= 3)
            {
                chart2.Series[0].Points.AddXY(time, split[4]);
                if (chart2.Series[0].Points.Count >100) chart2.Series[0].Points.RemoveAt(0);

                time += 1.0;
                altitudeChart.AxisX.Minimum =chart2.Series[0].Points[0].XValue;
                altitudeChart.AxisX.Maximum =time;

                double minY = 0;
                double maxY = 1800;

                //altitudeChart.AxisY.Minim um = minY;
                altitudeChart.AxisY.Maximum = maxY;
                altitudeChart.AxisY.IsStartedFromZero = false;

                altitudeChart.AxisY.Interval = 400;
                altitudeChart.AxisY.IntervalOffset = minY % 10;
                altitudeChart.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;

                altitudeChart.AxisX.LabelStyle.Format = "0.0";

                if (chart2.Series[0].Points.Count > 0)
                {
                    //sabit olması için int olarak Cast ediyoruz
                    double currentMaxY = (int)chart2.Series[0].Points.Max(p => p.YValues[0]);
                    double currentMinY = (int)chart2.Series[0].Points.Min(p => p.YValues[0]);

                    if (currentMaxY > maxY) altitudeChart.AxisY.Maximum = currentMaxY + 500;

                    if (currentMinY < minY) altitudeChart.AxisY.Minimum = currentMinY - 200;
                }
            }
        }

        private void GyroGrapth()
        {
            if (chart3?.ChartAreas.Count > 0)
            {
                var altitudeChart = chart3.ChartAreas[0];
                if (rocketList != null && split != null && split.Length > 7)
                {
                    // Ensure there are enough series in the chart
                    if (chart3.Series.Count < 3)
                    {
                        for (int i = chart3.Series.Count; i < 3; i++)
                        {
                            var series = new System.Windows.Forms.DataVisualization.Charting.Series
                            {
                                Name = $"Series{i + 1}",
                                ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                                Color = i == 0 ? Color.Red : (i == 1 ? Color.Yellow : Color.Blue),
                                BorderWidth = 2
                            };
                            chart3.Series.Add(series);
                        }
                    }

                    // Add data points to each series
                    chart3.Series[0]?.Points.AddXY(time, split[5]); // Example for the first series
                    chart3.Series[1]?.Points.AddXY(time, split[6]); // Example for the second series
                    chart3.Series[2]?.Points.AddXY(time, split[7]); // Example for the third series

                    // Remove old points to keep the chart from growing indefinitely
                    foreach (var series in chart3.Series)
                    {
                        if (series.Points.Count > 100)
                            series.Points.RemoveAt(0);
                    }

                    time += 1.0;
                    altitudeChart.AxisX.Minimum = chart3.Series[0]?.Points[0].XValue ?? 0;
                    altitudeChart.AxisX.Maximum = time;

                    double minY = 0;
                    double maxY = 100;

                    altitudeChart.AxisY.Maximum = maxY;
                    altitudeChart.AxisY.IsStartedFromZero = false;
                    altitudeChart.AxisY.Interval = 40;
                    altitudeChart.AxisY.IntervalOffset = minY % 10;
                    altitudeChart.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
                    altitudeChart.AxisX.LabelStyle.Format = "0.0";

                    if (chart3.Series[0]?.Points.Count > 0)
                    {
                        double currentMaxY = chart3.Series.Max(s => s.Points.Max(p => p.YValues[0]));
                        double currentMinY = chart3.Series.Min(s => s.Points.Min(p => p.YValues[0]));

                        if (currentMaxY > maxY) altitudeChart.AxisY.Maximum = currentMaxY + 90;
                        if (currentMinY < minY) altitudeChart.AxisY.Minimum = currentMinY - 90;
                    }

                    if (chart3.Legends.Count >= 3)
                    {
                        chart3.Legends[0].Position = new System.Windows.Forms.DataVisualization.Charting.ElementPosition(85, 5, 5, 5);
                        chart3.Legends[1].Position = new System.Windows.Forms.DataVisualization.Charting.ElementPosition(90, 5, 5, 5);
                        chart3.Legends[2].Position = new System.Windows.Forms.DataVisualization.Charting.ElementPosition(95, 5, 5, 5);
                    }
                }
            }
        }

        #endregion
        #region EKSEN
        ////EKSEN DEĞİŞKENLERİ
        bool monitorLoaded = false;
        bool moveForm = false;
        int moveOffsetX = 0;
        int moveOffsetY = 0;
        BatuGL.VAO_TRIANGLES modelVAO = null; // 3d model vertex array object
        STL_Tools.Vector3 minPos = new STL_Tools.Vector3();
        STL_Tools.Vector3 maxPos = new STL_Tools.Vector3();
        public const float kScaleFactor = 5.0f;
        public double x = 0.0, y = 0.0, z = 0.0;

        public void glControl1_Load(object sender, EventArgs e)
        {
            glControl1.AllowDrop = true;
            monitorLoaded = true;
            glControl1.MakeCurrent();
            GL.ClearColor(Color.Black);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }

        private void SendData_Click(object sender, EventArgs e)
        {
            if (Hakem.IsOpen)
            {
                if (!sendDataState)
                {
                    sendDataState= true;
                    timer1.Start();
                    sendDataBtn.Text = "Veri Gönderme";
                    sendDataBtn.TextAlign = ContentAlignment.MiddleRight;
                    sendDataBtn.BackColor = Color.DarkRed;
                }
                else
                {
                    sendDataState = false;
                    timer1.Stop();
                    sendDataBtn.Text = "Veri Gönder";
                    sendDataBtn.TextAlign = ContentAlignment.MiddleRight;
                    sendDataBtn.BackColor = Color.MediumSlateBlue;
                }
            }
            else
            {
                MessageBox.Show("Hakem Portu Bağlantısını Kontrol Ediniz", "Uyarı", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            }
        }

        private void ConfigureBasicLighting(Color modelColor)
        {
            float[] light_1 = new float[] {
            0.2f * modelColor.R / 255.0f,
            0.2f * modelColor.G / 255.0f,
            0.2f * modelColor.B / 255.0f,
            1.0f };
            float[] light_2 = new float[] {
            10.0f * modelColor.R / 255.0f,
            10.0f * modelColor.G / 255.0f,
            10.0f * modelColor.B / 255.0f,
            1.0f };
            float[] specref = new float[] {
                0.2f * modelColor.R / 255.0f,
                0.2f * modelColor.G / 255.0f,
                0.2f * modelColor.B / 255.0f,
                1.0f };
            float[] specular_0 = new float[] { -1.0f, -1.0f, 1.0f, 1.0f };
            float[] specular_1 = new float[] { 1.0f, -1.0f, 1.0f, 1.0f };
            //float[] lightPos_0 = new float[] { 1000f, 1000f, -200.0f, 0.0f };
            float[] lightPos_1 = new float[] { -1000f, -1000f, -200.0f, 0.0f };

            GL.Enable(EnableCap.Lighting);
            /* light 0 */

            GL.Light(LightName.Light0, LightParameter.Ambient, light_1);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_2);
            GL.Light(LightName.Light0, LightParameter.Specular, specular_0);
            GL.Light(LightName.Light0, LightParameter.Position, lightPos_1);
            GL.Enable(EnableCap.Light0);
            /* light 1 */
            GL.Light(LightName.Light1, LightParameter.Ambient, light_1);
            GL.Light(LightName.Light1, LightParameter.Diffuse, light_2);
            GL.Light(LightName.Light1, LightParameter.Specular, specular_1);
            GL.Light(LightName.Light1, LightParameter.Position, lightPos_1);
            GL.Enable(EnableCap.Light1);
            /*material settings  */
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.AmbientAndDiffuse);
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, specref);
            GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 10);
            GL.Enable(EnableCap.Normalize);
        }


        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!monitorLoaded)
                return;

            BatuGL.Configure(glControl1, BatuGL.Ortho_Mode.CENTER);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.PushMatrix();
            BatuGL.Draw_WCS();


            if (clearClick == true)
            {
                GL.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);
                GL.Rotate(0, 1.0, 0.0, 0.0);
                GL.Rotate(0, 0.0, 0.0, 1.0);
                GL.Rotate(0, 0.0, 1.0, 0.0);
                clearClick = false;
            } 
            else
            {
                //default 
                GL.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);
                GL.Rotate(x, 1.0, 0.0, 0.0);
                GL.Rotate(y, 0.0, 0.0, 1.0);
                GL.Rotate(z, 0.0, 1.0, 0.0);
            }



            float scaleValue = 1.8f; // İstediğiniz büyüklük faktörü
            GL.Scale(scaleValue, scaleValue, scaleValue);

            if (modelVAO != null)
            {
                ConfigureBasicLighting(modelVAO.color);
                GL.Translate(-minPos.x, -minPos.y, -minPos.z); //cismin başlangıç konumu için
                GL.Translate(-(maxPos.x - minPos.x) / 2.0f, -(maxPos.y - minPos.y) / 2.0f, -(maxPos.z - minPos.z) / 2.0f); //cismin merkezi için
                if (modelVAO != null) modelVAO.Draw();
            }
            GL.PopMatrix();
            glControl1.SwapBuffers();
        }


        private void ReadSelectedFile(string fileName)
        {
            STLReader stlReader = new STLReader(fileName);
            TriangleMesh[] meshArray = stlReader.ReadFile();
            modelVAO = new BatuGL.VAO_TRIANGLES();
            modelVAO.parameterArray = STLExport.Get_Mesh_Vertices(meshArray);
            modelVAO.normalArray = STLExport.Get_Mesh_Normals(meshArray);
            modelVAO.color = Color.Crimson;
            minPos = stlReader.GetMinMeshPosition(meshArray);
            maxPos = stlReader.GetMaxMeshPosition(meshArray);
            //orb.Reset_Orientation();
            //orb.Reset_Pan();
            //orb.Reset_Scale();
            if (stlReader.Get_Process_Error())
            {
                modelVAO = null;
                /* if there is an error, deinitialize the gl monitor to clear the screen */
                Invoke((MethodInvoker)delegate
                {
                    BatuGL.Configure(glControl1, BatuGL.Ortho_Mode.CENTER);
                    glControl1.SwapBuffers();
                });
            }
            else
            {
                // OpenGL kontrollerini güvenli bir şekilde güncelle
                Invoke((MethodInvoker)delegate
                {
                    glControl1.Invalidate();
                });
            }
        }


        private void FileMenuImportBt_Click(object sender, EventArgs e)
        {
            OpenFileDialog newFileDialog = new OpenFileDialog();
            newFileDialog.Filter = "STL Files|*.stl;*.txt;";
            string stlfineName = "C:\\Users\\yasar k\\Desktop\\Yer İstasyonu\\Yer_istasyonu\\STL_Tools\\Rocket.STL";
            //if (newFileDialog.ShowDialog() == DialogResult.OK)
            //{
            //ReadSelectedFile(newFileDialog.FileName);
            //}
            if (File.Exists(stlfineName))
            {
                // Dosya varsa işlemlere devam et
                ReadSelectedFile(stlfineName);
            }
            else
            {
                MessageBox.Show("Dosya bulunamadı.");
            }
        }

        #endregion
        #region GPS
        public class GPSData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
        GPSData gpsData1 = new GPSData();
        GPSData gpsData2 = new GPSData();
        

        private void InitializeGPS()
        {
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.Position =new PointLatLng(41.0082, 28.9784);
            gMapControl1.MinZoom = 3;
            gMapControl1.MaxZoom = 30;
            gMapControl1.Zoom = 8;

            MarkerOverlar = new GMapOverlay("markers");
            gMapControl1.Overlays.Add(MarkerOverlar);

            ////Görev yükü
            ///
            gMapControl2.DragButton = MouseButtons.Left;
            gMapControl2.MapProvider = GMapProviders.GoogleMap;
            gMapControl2.Position = new PointLatLng(41.0082, 28.9784);
            gMapControl2.MinZoom =3;
            gMapControl2.MaxZoom = 30;
            gMapControl2.Zoom = 8;

            MarkerOverlar2 = new GMapOverlay("markers");
            gMapControl2.Overlays.Add(MarkerOverlar2);

        }


        private void DisplayGPS(GMapControl gMapControl, GMapOverlay markersOverlay, String latitudeText, String longitudeText)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

            if (double.TryParse(latitudeText.Replace(',', '.'), NumberStyles.Float, culture, out double latitude) &&
                double.TryParse(longitudeText.Replace(',', '.'), NumberStyles.Float, culture, out double longitude))
            {
                if (latitude != 0.0 && longitude != 0.0)
                {
                    PointLatLng point = new PointLatLng(latitude, longitude);
                    var marker = new GMarkerGoogle(point, GMarkerGoogleType.red_dot);

                    // Tooltip bilgilendirme kutusu özelleştirme (opsiyonel)
                    marker.ToolTipText = $"Latitude: {point.Lat},\nLongitude: {point.Lng}";
                    var tooltip = new GMapToolTip(marker)
                    {
                        Fill = new SolidBrush(Color.Black),
                        Foreground = new SolidBrush(Color.White),
                        Offset = new Point(20, -40)
                    };
                    marker.ToolTip = tooltip;

                    markersOverlay.Markers.Clear();
                    markersOverlay.Markers.Add(marker);
                    gMapControl.Position = point;
                    gMapControl.Refresh();
                }
            }
        }

        #endregion
    }
}
