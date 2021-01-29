

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Controls;
using System.Diagnostics;
using System.Timers;

namespace PaddleXCsharp
{
    class UsbDeviceSource
    {
        // 版本
        private static Version sfnc2_0_0 = new Version(2, 0, 0);

        private FilterInfoCollection videoDevices;//枚举所有摄像头设备

        private VideoCaptureDevice videoSource; //视频的来源选择

        private List<VideoCaptureDevice> videoSourceList = new List<VideoCaptureDevice>();

        public VideoSourcePlayer videoSourcePlayer ;  //AForge控制控件

        private bool is_record_video = false;  //是否开始录像

        private Accord.Video.DirectShow.CameraControlFlags camctrlflag;//相机参数获取


        private int tick_num = 0;

        public bool DeviceExist = false; //设备是否存在标志

        public NewFrameEventHandler callBackHandler;

        public VideoSourcePlayer.NewFrameHandler videoSourcePlayerCallBackHandler;

        private FilterInfoCollection allCameraInfos;

        private Stopwatch stopWatch = null;

        private System.Timers.Timer timer;

        public string fpsLabel = "";

        //public List<string> items = new List<string>();  //设备信息列表

        public UsbDeviceSource() {
            //initCamera();
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(timer_Tick);
            timer.AutoReset = true;
        }

        // 相机个数
        public int CameraNum()
        {
            return videoDevices.Count;
        }

        // 枚举相机
        //public List<string> CameraEnum()
        //{
        //    items = new List<string>();
        //    foreach (FilterInfo device in videoDevices)
        //    {
        //        items.Add(device.Name);
        //    }
        //    return items;
        //}

        // 枚举相机
        public FilterInfoCollection CameraEnum()
        {
            return videoDevices;
        }

        // 设置相机参数
        public void GetCameraProperty(CameraControlProperty a, out int b)
        {
            this.videoSource.GetCameraProperty(a, out b, out camctrlflag);
        }

        // 获取相机参数
        public bool SetCameraProperty(CameraControlProperty property, int value, CameraControlFlags controlFlags) 
        {
            return this.videoSource.SetCameraProperty(property, value, controlFlags);
        }

        // 开始采集
        public void StartGrabbing()
        {
            if (videoSource != null && !videoSource.IsRunning)
            {
                videoSource.Start();
            }
        }

        // 停止采集
        public void StopGrabbing()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                this.videoSource.SignalToStop();
                this.videoSource.WaitForStop();
            }
        }

        //USB相机初始化，查找USB相机设备
        public FilterInfoCollection initCamera()
        {
            try
            {
                if (videoDevices == null)
                {
                    //枚举所有视频输入设备
                    videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                    // 添加设备到List，此处省略，在外部操作。

                    if (videoDevices.Count != 0)
                    {
                        DeviceExist = true;
                        //throw new ApplicationException("没有发现USB相机！");
                    }
                    else {
                        throw new ApplicationException();
                    }
                }
            }
            catch (ApplicationException)
            {
                DeviceExist = false;
                videoDevices = null;
            }
            return videoDevices;
        }

        // 多相机初始化
        public List<VideoCaptureDevice> MultiCameraInit(int Num)
        {
            for (int i = 0; i < Num; i++)
            {
                videoSourceList.Add(new VideoCaptureDevice(videoDevices[i].MonikerString));
                if (true == videoSourceList[i].IsRunning)
                {
                    videoSourceList[i].Stop();
                }
                //打开相机
                videoSourceList[i].Start();
            }
            return this.videoSourceList;
        }
 
        //关闭相机对象 
        public bool close()
        {
            //if (videoSource != null && videoSource.IsRunning)
            //{
            //    //videoSource.SignalToStop();
            //    //videoSource = null;
            //    videoSource.SignalToStop();
            //    videoSource.WaitForStop();
            //    //videoSourcePlayer.SignalToStop();
            //    //videoSourcePlayer.WaitForStop();
            //    System.GC.Collect();
            //}
            if (videoSource != null)
            {
                videoSource.SignalToStop();

                // wait ~ 3 seconds
                for (int i = 0; i < 30; i++)
                {
                    if (!videoSource.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                if (videoSource.IsRunning)
                {
                    videoSource.Stop();
                }

                videoSource = null;
            }

            return true;
        }


        //打开相机
        public VideoCaptureDevice open(int selectIndex) {
            if (DeviceExist)
            {
                close();
                try
                {
                    //启动USB相机
                    if (selectIndex < videoDevices.Count)
                    {
                        this.videoSource = new VideoCaptureDevice(videoDevices[selectIndex].MonikerString);//连接摄像头
                        videoSourcePlayer = new VideoSourcePlayer();
                        videoSource.NewFrame += new NewFrameEventHandler(callBackHandler);//捕获画面事件
                        videoSourcePlayer.NewFrame += new Accord.Controls.VideoSourcePlayer.NewFrameHandler(videoSourcePlayerCallBackHandler);
                        videoSourcePlayer.VideoSource = videoSource;
                        videoSourcePlayer.Start();
                        timer.Enabled = true;
                        timer.Start();
                        //videoSourcePlayer.Start();
                    }
                    else {
                        throw new ApplicationException("打开相机失败！");
                    }
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            return this.videoSource;
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

        private void MV_CC_GetOneFrameTimeout_NET(ref Bitmap stFrameInfo,int nMesc)
        {
            Thread.Sleep(nMesc);
            stFrameInfo = this.videoSourcePlayer.GetCurrentVideoFrame();
        }


        // On timer event - gather statistics
        private void timer_Tick(object sender, ElapsedEventArgs e)
        {
            IVideoSource videoSource = videoSourcePlayer.VideoSource;

            if (videoSource != null)
            {
                // get number of frames since the last timer tick
                int framesReceived = videoSource.FramesReceived;

                if (stopWatch == null)
                {
                    stopWatch = new Stopwatch();
                    stopWatch.Start();
                }
                else
                {
                    stopWatch.Stop();

                    float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
                    fpsLabel = fps.ToString("F2") + " fps";

                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
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
