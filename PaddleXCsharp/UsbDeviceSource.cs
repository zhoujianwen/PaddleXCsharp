using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaddleXCsharp
{
    class UsbDeviceSource
    {
        public FilterInfoCollection videoDevices;//枚举所有摄像头设备
        public VideoCaptureDevice videoSource; //视频的来源选择
        public VideoSourcePlayer videoSourcePlayer ;  //AForge控制控件
        //private VideoFileWriter writer;   //写入到视频
        public bool is_record_video = false;  //是否开始录像
        public AForge.Video.DirectShow.CameraControlFlags camctrlflag;//相机参数获取
        public System.Timers.Timer timer_count;
        public int tick_num = 0;
        public bool DeviceExist = false; //设备是否存在标志
        public NewFrameEventHandler callBackHandler;
        FilterInfoCollection allCameraInfos;

        public UsbDeviceSource() {
           //initCamera();
        }

        public List<string> items;  //设备信息列表

        // 相机个数
        public int CameraNum()
        {
            return videoDevices.Count;
        }

        // 枚举相机
        public FilterInfoCollection CameraEnum()
        {
            // 相机个数
            return videoDevices;
        }

        // 开始采集
        public void StartGrabbing()
        {
            if (videoSource != null)
            {
                //camera.StreamGrabber.Start();//
            }
        }

        // 停止采集
        public void StopGrabbing()
        {
            if (videoSource != null)
            {
                //camera.StreamGrabber.Stop();
            }
        }

        //USB相机初始化
        public void initCamera()
        {
            try
            {
                if (videoDevices == null)
                {
                    videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    videoSource = new VideoCaptureDevice();
                    videoSourcePlayer = new AForge.Controls.VideoSourcePlayer();
                    items = new List<string>();
                    //writer = new VideoFileWriter();   
                    if (videoDevices.Count == 0)
                        throw new ApplicationException("没有发现USB摄像机！");

                    foreach (FilterInfo device in videoDevices)
                    {
                        items.Add(device.Name);
                    }
                }
                DeviceExist = true;
            }
            catch (ApplicationException)
            {
                DeviceExist = false;
                items.Add("没有设备");
                videoDevices = null;
            }
        }

        //关闭相机对象 
        public bool close()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource = null;
                System.GC.Collect();
            }
            return true;
        }

        //打开相机
        public bool open() {
            if (DeviceExist)
            {
                close();
                try
                {
                    //启动USB相机
                    this.videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);//连接摄像头
                    videoSource.NewFrame += new NewFrameEventHandler(callBackHandler);//捕获画面事件
                    videoSourcePlayer.VideoSource = videoSource;
                    videoSource.Start();
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }

       

        System.Drawing.Bitmap bitmap;
        private void VideoDev_NewFrame(object sender, ref Bitmap image)
        {
            if (is_record_video)
            {
                bitmap = this.videoSourcePlayer.GetCurrentVideoFrame();
                //videoWriter.WriteVideoFrame(bmp1);
            }
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnScannerImage_Click(object sender, EventArgs e)
        {
            if (videoSource == null)
                return;
            bitmap = videoSourcePlayer.GetCurrentVideoFrame();
            string fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ff") + ".jpg";

            bitmap.Save(Application.StartupPath + "\\" + fileName, ImageFormat.Jpeg);
            bitmap.Dispose();
        }


        //计时器响应函数
        public void tick_count(object source, System.Timers.ElapsedEventArgs e)
            {
                tick_num++;
                int temp = tick_num;

                int sec = temp % 60;

                int min = temp / 60;
                if (60 == min)
                {
                    min = 0;
                    min++;
                }

                int hour = min / 60;

                String tick = hour.ToString() + "：" + min.ToString() + "：" + sec.ToString();
                //this.label4.Text = tick;
            }

        }
}
