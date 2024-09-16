using Accord.Video.FFMPEG;
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Yer_İstasyonu
{
    internal class Camera_Module : IDisposable
    {
        private VideoCaptureDevice videoSource;
        private VideoFileWriter videoWriter;
        private PictureBox pictureBox;
        private bool isRecording = false;
        private DateTime? firstFrameTime = null;

        public bool IsRecording
        {
            get { return isRecording; }
        }

        private string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Konuralp_video_kayıtları");
        private int recordingIndex = 1;

        public Camera_Module()
        {
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }
        }

        //combobox içine bilgisayardaki aktif olan video aktarım akışlarını ekler 
        public void AddCombobox(ComboBox comboBox)
        {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
            {
                comboBox.Items.Add(device.Name);
            }
        }

        //comboboxtan seçilen item'ın indexini ve picturebox parametre alır index numarasından o combobox içindeki elemanla eşleşme yapıp 
        //görüntü akışı secilmesine, picturebox ise video görüntüsünün ekrana yansıtmak için parametre alıyor.
        public void Connect(int cameraIndex, PictureBox targetPictureBox)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource = null;
            }


            pictureBox = targetPictureBox;
            //spesifik bir kamera seçip görüntü akışı(stream) başlatmak için gereken kod
            videoSource = new VideoCaptureDevice(new FilterInfoCollection(FilterCategory.VideoInputDevice)[cameraIndex].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start(); //videoyu başlatma
        }

        public void Disconnect(PictureBox pcb)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource = null;
            }

            if (videoWriter != null)
            {
                videoWriter.Close();
                videoWriter.Dispose();
            }
            pcb.Image = null;
        }

        public void StartRecording()
        {
            if (videoSource == null)
            {
                throw new InvalidOperationException("Kamera bağlı değil.");
            }

            string videoFileName = $"video{recordingIndex}.mkv";
            string outputFilePath = Path.Combine(baseDirectory, videoFileName);

            // Dosya varsa indeksi artır ve videoyu dosya olarak kaydetme işlemlerini gerçekleştir
            while (File.Exists(outputFilePath))
            {
                recordingIndex++;
                videoFileName = $"video{recordingIndex}.mkv";
                outputFilePath = Path.Combine(baseDirectory, videoFileName);
            }

            videoWriter = new VideoFileWriter();
            videoWriter.Open(outputFilePath, pictureBox.Image.Width, pictureBox.Image.Height, 30, VideoCodec.MPEG4, 10000000); // ortalama bitrate: 10000000

            isRecording = true;
            firstFrameTime = null; 
        }

        //video kaydını durdurma
        public void StopRecording()
        {
            if (videoWriter != null)
            {
                videoWriter.Close();
                videoWriter.Dispose();
            }

            recordingIndex++; //her bir video işlemi durdurulup başlatıldığında videonun ismi otomatik değişecektir 'Örn: video1.mkv, video2.mkv, ...'
            isRecording = false;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (eventArgs.Frame != null)
                {
                    // frame klonlama
                    Bitmap clonedFrame = (Bitmap)eventArgs.Frame.Clone();

                    // pictureboxtaki görüntüyü güncelleme
                    if (pictureBox.InvokeRequired)
                    {
                        pictureBox.Invoke(new Action(() => pictureBox.Image = clonedFrame));
                    }
                    else
                    {
                        pictureBox.Image = clonedFrame;
                    }

                    // VideoWriter ile ekrandaki görüntüyü kaydetmemizi sağlar
                    if (videoWriter != null && videoWriter.IsOpen)
                    {
                        //video kaydetmeyi eğer butona basılırsa o anı zaman dilimi olarak alıcak ve o andan itibaren video olarak kaydedecek
                        //aksi halde sadece canlı yayın tarzında kamera görüntüsü gözükecek
                        if (firstFrameTime == null)
                        {
                            firstFrameTime = DateTime.Now;
                        }
                        TimeSpan frameTime = DateTime.Now - firstFrameTime.Value;
                        using (Bitmap frameToWrite = (Bitmap)eventArgs.Frame.Clone())
                        {
                            videoWriter.WriteVideoFrame(frameToWrite, frameTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata işleme
                Console.WriteLine("Hata: " + ex.Message);
            }
        }

        
        public void Dispose()
        {
            StopRecording();
        }
    }
}
