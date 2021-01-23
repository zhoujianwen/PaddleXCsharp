using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public bool DeviceExist = false;

        public UsbDeviceSource() {
            initCamera();
        }

        public List<string> items;  //设备信息列表

        //初始化USB摄像机
        public void initCamera()
        {
            try
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
                DeviceExist = true;
            }
            catch (ApplicationException)
            {
                DeviceExist = false;
                items.Add("没有设备");
                videoDevices = null;
            }
        }

        //close the device safely 
        public bool close()
        {
            if (videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                return true;
            }
            else {
                return false;
            }
        }

        public bool open() {
            if (DeviceExist)
            {
                close();
                try
                {
                    //启动USB相机
                    this.videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(show_video);
                    videoSourcePlayer.VideoSource = videoSource;
                    videoSource.Start();
                    //Func<bool> delFunc = () => {

                    //    videoSourcePlayer.Invoke(new Action(delegate () {  }));
                    //    return true;
                    //};
                    //IAsyncResult result = delFunc.BeginInvoke(null, null);
                    //while (!result.IsCompleted)
                    //{
                    //    DeviceExist = false;
                    //}
                    //DeviceExist = delFunc.EndInvoke(result);
                    DeviceExist = true;
                    return DeviceExist;
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return DeviceExist;
        }

        //新帧的触发函数
        SingleCamera singleCamera;
        private void show_video(object sender, NewFrameEventArgs eventArgs)
        {
         
            Bitmap bitmap = eventArgs.Frame;  //获取到一帧图像
            singleCamera = ((SingleCamera)LoginForm.listForm["SingleCamera"]);
            singleCamera.pictureBox1.Invoke(new Action(delegate () { singleCamera.pictureBox1.Image = Image.FromHbitmap(bitmap.GetHbitmap());}));
            //不建议跨线程访问，上下文频繁切换造成资源开销严重，将show_video方法迁移至SingleCamera.cs文件内实现。
            //if (is_record_video)
            //{
            //    writer.WriteVideoFrame(bitmap);
            //}

        }

        System.Drawing.Bitmap tbmp;
        private void VideoDev_NewFrame(object sender, ref Bitmap image)
        {
            if (is_record_video)
            {
                tbmp = this.videoSourcePlayer.GetCurrentVideoFrame();
                //videoWriter.WriteVideoFrame(bmp1);
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
