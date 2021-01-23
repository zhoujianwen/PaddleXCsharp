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
                    videoSource.Start();
                    videoSourcePlayer.VideoSource = videoSource;

                    Func<bool> delFunc = () => {
                        videoSourcePlayer.Start();
                        return true;
                    };
                    IAsyncResult result = delFunc.BeginInvoke(null, null);
                    while (!result.IsCompleted)
                    {
                        DeviceExist = false;
                    }
                    DeviceExist = delFunc.EndInvoke(result);
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
        private void show_video(object sender, NewFrameEventArgs eventArgs)
        {
         
            Bitmap bitmap = eventArgs.Frame;  //获取到一帧图像
            SingleCamera singleCamera = ((SingleCamera)LoginForm.listForm["SingleCamera"]);
            singleCamera.pictureBox1.Image = Image.FromHbitmap(bitmap.GetHbitmap());
            //if (is_record_video)
            //{
            //    writer.WriteVideoFrame(bitmap);
            //}
            
        }


    }
}
