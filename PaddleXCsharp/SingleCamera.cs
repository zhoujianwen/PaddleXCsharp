using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MvCamCtrl.NET;  // 海康相机
using HIKDeviceSource; // 海康
using Basler.Pylon; // Basler 相机
using BaslerDeviceSource; // Basler
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Microsoft.WindowsAPICodePack.Dialogs;

//USB相机
#region  
using Accord.Video.DirectShow;
using Accord.Video;
using System.Diagnostics;
using System.Configuration;
using Sunny.UI;

#endregion

namespace PaddleXCsharp
{
    public partial class SingleCamera: Form
    {
        private delegate void UpdateUI();  // 声明委托

        /* ================================= basler相机 ================================= */
        BaslerCamera baslerCamera = new BaslerCamera();
        Camera camera1 = null;
        Thread baslerGrabThread = null;
        private PixelDataConverter converter;// = new PixelDataConverter(); // basler里用于将相机采集的图像转换成位图

        bool baslerCanGrab = false;    // 控制相机是否Grab
        bool chooseBasler = false;     // Basler相机打开标志

        /* ================================= 海康相机 ================================= */
        HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();
        MyCamera camera2 = null;
        Thread hikGrabThread = null;
         
        bool hikCanGrab = false;  // 控制相机是否Grab
        bool chooseHIK = false;   // 海康相机打开标志

        /* ================================= USB相机 ================================= */
        private UsbDeviceSource usbDeviceSource = new UsbDeviceSource();
        VideoCaptureDevice camera3 = null;
        bool usbCanGrab = false;  // 控制USB相机是否Grab
        bool chooseUSB = false;   // USB相机打开标志
        Thread usbGrabThread = null;
        

        /// <summary>
        /// 同步事件，新帧的触发函数
        /// 20190515 by hanfre 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="NewFrameEventArgs"></param>
        Bitmap bitmap = null;
        //Mutex mutexLock = new Mutex();
        int n = 0;
        //Queue<Bitmap> bitmapList = new Queue<Bitmap>();
        static Object mutexLock = new Object();
        private void videoSourcePlayer_NewFrameReceived(object sender, NewFrameEventArgs eventArgs)
        {
            //bitmapList.Enqueue((Bitmap)eventArgs.Frame.Clone());
            //Thread.Sleep(1000);
            //lock (this)
            //{
            if (Monitor.TryEnter(mutexLock, TimeSpan.FromSeconds(15)))
            {
                //mutexLock.WaitOne();
                bitmap = (Bitmap)eventArgs.Frame.Clone();  //获取一帧图像

                if (isInference) { bitmap = Inference(bitmap); }
                if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                {
                    UpdateUI update = delegate
                    {

                        DateTime now = DateTime.Now;
                        Graphics g = Graphics.FromImage(bitmap);
                    // paint current time
                    SolidBrush brush = new SolidBrush(Color.Red);
                        g.DrawString(now.ToString(), this.Font, brush, new PointF(5, 5));
                        g.DrawString(string.Format("ID:{0}", n++), this.Font, brush, new PointF(5, this.pictureBox1.Height - 200));
                        this.pictureBox1.Image = bitmap;
                        brush.Dispose();
                        g.Dispose();

                    };
                    pictureBox1.BeginInvoke(update);
                } 
            }
            Monitor.Exit(mutexLock);
            //mutexLock.ReleaseMutex();
            //else
            //{
            //    pictureBox1.Image = bitmap;
            //}
            //}

        }

        // New frame received by the player
        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            //DateTime now = DateTime.Now;
            //Graphics g = Graphics.FromImage(image);

            //// paint current time
            //SolidBrush brush = new SolidBrush(Color.Red);
            //g.DrawString(now.ToString(), this.Font, brush, new PointF(5, 5));
            //g.DrawString(usbDeviceSource.fpsLabel, this.Font, brush, new PointF(5, this.pictureBox1.Height-200));
            //this.pictureBox1.Image = image;
            //brush.Dispose();
            //g.Dispose();
        }



        ///// <summary>保存图片框的句柄</summary>
        //private IntPtr pbHWND;
        ///// <summary>临时图片，用于保存到视频</summary>
        //private Bitmap tmpBmp;
        //pbHWND = pBCamera.Handle;
        //tmpBmp = new Bitmap(640,480);
        //private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        //{
        //    //Bitmap bmp = (Bitmap)eventArgs.Frame.Clone();    //获取到一帧图像
        //    Graphics g = Graphics.FromHwnd(pbHWND);
        //    g.DrawImage(eventArgs.Frame, 0, 0, eventArgs.Frame.Width, eventArgs.Frame.Height);
        //    g.Dispose();
        //    if (is_record_video)
        //    {

        //        Graphics bmpG = Graphics.FromImage(tmpBmp);
        //        bmpG.DrawImage(eventArgs.Frame, 0, 0, eventArgs.Frame.Width, eventArgs.Frame.Height);
        //        writer.WriteVideoFrame(tmpBmp);
        //        bmpG.Dispose();
        //    }
        //}

        // 用于从驱动获取图像的缓存

        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver;

        /* ================================= inference ================================= */
        #region 接口定义及参数
        int modelType = 1;  // 模型的类型  0：分类模型；1：检测模型；2：分割模型
        string modelPath = ""; // 模型目录路径
        bool useGPU = false;  // 是否使用GPU
        bool useTrt = false;  // 是否使用TensorRT
        bool useMkl = true;  // 是否使用MKLDNN加速模型在CPU上的预测性能
        int mklThreadNum = 8; // 使用MKLDNN时，线程数量
        int gpuID = 0; // 使用GPU的ID号
        string key = ""; //模型解密密钥，此参数用于加载加密的PaddleX模型时使用
        bool useIrOptim = true; // 是否加速模型后进行图优化
        bool visualize = false;
        bool isInference = false;  // 是否进行推理   
        static IntPtr model; // 模型

        // 目标物种类，需根据实际情况修改！
        string[] category = { "background", "xiaoduxiong"};

        // 定义CreatePaddlexModel接口
        [DllImport("detector.dll", EntryPoint = "CreatePaddlexModel", CharSet = CharSet.Ansi)]
        static extern IntPtr CreatePaddlexModel(ref int modelType, 
                                                string modelPath, 
                                                bool useGPU, 
                                                bool useTrt, 
                                                bool useMkl, 
                                                int mklThreadNum, 
                                                int gpuID, 
                                                string key, 
                                                bool useIrOptim);
        
        // 定义分类接口
        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexClsPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern bool PaddlexClsPredict(IntPtr model, byte[] image, int height, int width, int channels, out int categoryID, out float score);

        // 定义检测接口
        [DllImport("detector.dll", EntryPoint = "PaddlexDetPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern bool PaddlexDetPredict(IntPtr model,[MarshalAs(UnmanagedType.LPArray)]  byte[] image, int height, int width, int channels, int max_box, float[] result, bool visualize);
        
        #endregion

        //// 定义语义分割接口
        //[DllImport("paddlex_inference.dll", EntryPoint = "PaddlexSegPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //static extern bool PaddlexSegPredict(IntPtr model, byte[] image, int height, int width, int channels, );

        public SingleCamera()
        {
            InitializeComponent();
            // 子线程安全访问窗体控件
            Control.CheckForIllegalCrossThreadCalls = false;
            this.bnLoadModel.Enabled = false;
            this.bnOpen.Enabled = false;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        // 选择所使用相机的类型
        private void BnEnum_Click(object sender, EventArgs e)
        {
            
            string type = cameraType.Text;
            if (type == "")
            {
                MessageBox.Show("请在初始化界面中选定相机类型", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (type == "海康相机")
            {
                chooseHIK = true;
                chooseBasler = false;
                chooseUSB = false;
                DeviceListAcq();
            }
            else if (type == "Basler相机")
            {
                chooseHIK = false;
                chooseBasler = true;
                chooseUSB = false;
                DeviceListAcq();
            }
            else if (type =="USB相机")
            {
                chooseHIK = false;
                chooseBasler = false;
                chooseUSB = true;
                DeviceListAcq();
            }    
        }

        // 枚举相机
        private void DeviceListAcq()
        {
            // 清空列表
            cbDeviceList.DataSource = null;
            cbDeviceList.Items.Clear();
            System.GC.Collect();

            // 枚举海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 相机数量
                    uint cameraNum = hIKVisionCamera.CameraNum();
                    // 枚举相机
                    List<string> items = hIKVisionCamera.EnumDevices();
                    for (int i = 0; i < cameraNum; i++)
                    {
                        cbDeviceList.Items.Add(items[i]);
                    }
                    // 选择第一项
                    if (cameraNum != 0)
                    {
                        cbDeviceList.SelectedIndex = 0;
                    }
                }
                catch
                {
                    MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // 枚举basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 返回相机数量
                    int cameraNum = baslerCamera.CameraNum();
                    // 枚举相机
                    List<ICameraInfo> items = baslerCamera.CameraEnum();
                    for (int i = 0; i < cameraNum; i++)
                    {
                        if (items[i][CameraInfoKey.DeviceType] == "BaslerGigE")
                        {
                            cbDeviceList.Items.Add("GigE: Basler " + items[i][CameraInfoKey.ModelName]);
                        }
                        else if (items[i][CameraInfoKey.DeviceType] == "BaslerUsb")
                        {
                            cbDeviceList.Items.Add("USB: Basler " + items[i][CameraInfoKey.ModelName]);
                        }
                    }
                    // 选择第一项
                    if (cameraNum != 0)
                    {
                        cbDeviceList.SelectedIndex = 0;
                    }
                }
                catch
                {
                    MessageBox.Show("枚举设备失败，请检查连接状态！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if((chooseUSB)&&(!chooseHIK)&&(!chooseBasler))
            {
                // 枚举USB相机
                try
                {
                    //连接相机
                    usbDeviceSource.initCamera();
                    usbDeviceSource.callBackHandler += videoSourcePlayer_NewFrameReceived; //回调函数处理视频帧
                    usbDeviceSource.videoSourcePlayerCallBackHandler += videoSourcePlayer_NewFrame;
                    if (usbDeviceSource.DeviceExist)
                    {
                        // 返回相机数量
                        int cameraNum = usbDeviceSource.CameraNum();

                        // 枚举相机
                        foreach (FilterInfo device in usbDeviceSource.CameraEnum())
                        {
                            cbDeviceList.Items.Add(device.Name);
                        }
                        // https://www.cnblogs.com/xiaoliangge/p/6006055.html
                        // 选择第一项
                        if (cameraNum != 0)
                        {
                            cbDeviceList.SelectedIndex = 0;
                        }
                        this.bnOpen.Enabled = true;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    this.bnOpen.Enabled = false;
                    cbDeviceList.Items.Add("枚举设备失败，请检查连接状态！");
                    cbDeviceList.SelectedIndex = 0;
                    MessageBox.Show("枚举设备失败，请检查连接状态！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
        }

        #region 控件使能
        private void SetCtrlWhenOpen()
        {
            bnOpen.Enabled = false;
            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            bnGetParam.Enabled = true;
            bnSetParam.Enabled = true;
            //usblabelinfo.Visible = false ;
        }
        private void SetCtrlWhenClose()
        {
            bnOpen.Enabled = true;
            bnClose.Enabled = false;
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            bnGetParam.Enabled = false;
            bnSetParam.Enabled = false;
            bnLoadModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;
            usblabelinfo.Visible = false;
        }
        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;
            bnLoadModel.Enabled = true;
            bnStartDetection.Enabled = true;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = true;
        }
        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            bnLoadModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;
            bnThreshold.Enabled = false;
        }
        #endregion


        // 启动设备
        private void BnOpen_Click(object sender, EventArgs e)
        {
            // 启动海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    camera2 = hIKVisionCamera.CameraInit(cbDeviceList.SelectedIndex);
                    // 获取参数
                    BnGetParam_Click(null, null);

                    // 控件操作
                    SetCtrlWhenOpen();
                }
                catch
                {

                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;
                }
            }

            // 启动basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 初始化所选相机
                    camera1 = baslerCamera.CameraInit(cbDeviceList.SelectedIndex);
                    // 获取参数
                    BnGetParam_Click(null, null);
                    // 控件操作
                    SetCtrlWhenOpen();
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if(chooseUSB&&(!chooseBasler) && (!chooseHIK))
            {
                //启动USB相机
                
                Func<VideoCaptureDevice> delFunc = () => {  camera3 = usbDeviceSource.open(cbDeviceList.SelectedIndex); return camera3;  };
                IAsyncResult result = delFunc.BeginInvoke(null, null);
                this.bnThreshold.Enabled = false;
                int count = 0;

                #region
                //Thread waitloading = new Thread(() => {
                //    count = 0;
                //    while (count < 2)
                //    {
                //        if (count < 4)
                //        {
                //            this.Invoke(new Action(() => { this.usblabelinfo.Text += "."; }));
                //        }
                //        else
                //        {
                //            count = 0;
                //            this.Invoke(new Action(() => { this.usblabelinfo.Text += "正在启动USB相机."; }));
                //        }
                //        count++;
                //        Thread.Sleep(500);
                //    }
                //    this.Invoke(new Action(() => { this.usblabelinfo.Visible = false; }));
                //});
                #endregion
                /*
                 result.IsCompleted可以判断异步委托是否执行完成，执行完成返回true
                 WaitOne方法自定义一个等待时间，如果在这个等待时间内异步委托没有执行完成，
                 那么就会执行 while里面的主线程的逻辑，反之就不会执行。
                https://zhoujianwen.blog.csdn.net/article/details/112385180
                C# Winform 跨线程更新UI控件常用方法汇总
                https://www.cnblogs.com/marshal-m/p/3201051.html
                 */
                this.usblabelinfo.Text = "正在启动USB相机";
                this.usblabelinfo.Visible = true;
                Thread loading = new Thread(() =>
                {
                    //异步操作判断USB相机是否启动成功
                    while (!result.AsyncWaitHandle.WaitOne(500)) //(!result.IsCompleted)
                    {
                        // 询问是否完成操作，否则做其它事情。
                        if (count < 4)
                        {
                            this.Invoke(new Action(() => { this.usblabelinfo.Text += ".";}));
                        }
                        else
                        {
                            count = 0;
                            this.Invoke(new Action(() => { this.usblabelinfo.Text = "正在启动USB相机."; }));
                        }
                        count++;
                    }

                    //获取委托函数返回结果
                    if (delFunc.EndInvoke(result)!=null)
                    {
                        // wait ~ 2 seconds
                        for(int i=0;i<4;i++)
                        { 
                            System.Threading.Thread.Sleep(500);
                            if (count < 4)
                            {
                                this.Invoke(new Action(() => { this.usblabelinfo.Text += "."; }));
                            }
                            else
                            {
                                count = 0;
                                this.Invoke(new Action(() => { this.usblabelinfo.Text = "正在启动USB相机."; }));
                            }
                            count++;
                            i++;
                        }
                        // 获取参数
                        BnGetParam_Click(null, null);
                        // 控件操作
                        SetCtrlWhenOpen();
                        usbCanGrab = true;
                        this.usblabelinfo.Visible = false;
                    }
                    else
                    {
                            this.Invoke(new Action(() => { this.usblabelinfo.Visible = false; }));
                     
                        MessageBox.Show("打开相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
                loading.IsBackground = true;
                loading.Start();
            }
        }

       
        // 关闭设备
        private void BnClose_Click(object sender, EventArgs e)
        {
            // 关闭海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 取流标志位清零
                    if (hikCanGrab == true)
                    {
                        hikCanGrab = false;
                        hikGrabThread.Join();
                    }
                    if (m_BufForDriver != IntPtr.Zero)
                    {
                        Marshal.Release(m_BufForDriver);
                    }
                    // 释放相机
                    hIKVisionCamera.DestroyCamera();
                    // 控件操作
                    SetCtrlWhenClose();
                }
                catch
                {
                    MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            // 关闭basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    if (baslerCanGrab == true)
                    {
                        baslerCanGrab = false;
                        baslerGrabThread.Join();
                    }
                    // 释放相机
                    baslerCamera.DestroyCamera();

                    // 控件操作
                    SetCtrlWhenClose();
                }
                catch
                {
                    MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (chooseUSB&&(!chooseBasler) && (!chooseHIK))
            {
                //关闭USB相机
                if (usbCanGrab)
                {
                    this.usblabelinfo.Text = "正在关闭USB相机";
                    this.usblabelinfo.Visible = true;

                    Func<bool> delFunc = () => { return usbDeviceSource.close(); };
                    IAsyncResult result = delFunc.BeginInvoke(null, null);
                    Thread loading = new Thread(() =>
                    {
                        int count = 0;
                        while (!result.IsCompleted) //(!result.AsyncWaitHandle.WaitOne(500))
                        {
                            // 询问是否完成操作，否则做其它事情。
                            if (count < 4)
                            {
                                this.Invoke(new Action(() => { usblabelinfo.Text += "."; this.Cursor = Cursors.WaitCursor; }));
                                
                            }
                            else
                            {
                                count = 0;
                                this.Invoke(new Action(() => { usblabelinfo.Text = "正在关闭USB相机";}));
                            }
                            count++;
                        }

                        //获取异步操作结果
                        if (delFunc.EndInvoke(result))
                        {
                            // wait ~ 3 seconds
                            count = 0;
                            for (int i = 0; i < 6; i++)
                            {
                                System.Threading.Thread.Sleep(500);
                                if (count < 4)
                                {
                                    this.Invoke(new Action(() => { this.usblabelinfo.Text += "."; }));
                                }
                                else
                                {
                                    count = 0;
                                    this.Invoke(new Action(() => { this.usblabelinfo.Text = "正在启动USB相机."; }));
                                }
                                count++;
                                i++;
                            }
                            // 控件操作
                            SetCtrlWhenClose();
                            // 清空pictureBox
                            this.Invoke(new Action(()=>{ this.Cursor = Cursors.Default; this.pictureBox1.Image = null;}));
                            usbCanGrab = false;
                        }
                        else
                        {
                            MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    });
                    loading.IsBackground = true;
                    loading.Start();
                }


            }
        }


        // 采集进程
        public void GrabThreadProcess()
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                int nRet = camera2.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Get PayloadSize failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                UInt32 nPayloadSize = stParam.nCurValue;
                if (nPayloadSize > m_nBufSizeForDriver)
                {
                    if (m_BufForDriver != IntPtr.Zero)
                    {
                        Marshal.Release(m_BufForDriver);
                    }
                    m_nBufSizeForDriver = nPayloadSize;
                    m_BufForDriver = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver);
                }
                if (m_BufForDriver == IntPtr.Zero)
                {
                    return;
                }

                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体
                //IntPtr pTemp = IntPtr.Zero;

                while (hikCanGrab)
                {
                    // 将海康数据类型转为Mat
                    nRet = camera2.MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref stFrameInfo, 1000); // m_BufForDriver为图像数据接收指针
                    //pTemp = m_BufForDriver;
                    byte[] byteImage = new byte[stFrameInfo.nHeight * stFrameInfo.nWidth];
                    Marshal.Copy(m_BufForDriver, byteImage, 0, stFrameInfo.nHeight * stFrameInfo.nWidth);
                    Mat matImage = new Mat(stFrameInfo.nHeight, stFrameInfo.nWidth, MatType.CV_8UC1, byteImage);
                    // 单通道图像转为三通道
                    Mat matImageNew = new Mat();
                    Cv2.CvtColor(matImage, matImageNew, ColorConversionCodes.GRAY2RGB);
                    Bitmap bitmap = matImageNew.ToBitmap();  // Mat转为Bitmap
                    // 是否进行推理
                    if (isInference) { bitmap = Inference(bitmap); }
                    if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                    {
                        UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                        pictureBox1.BeginInvoke(update);
                    }
                    else { pictureBox1.Image = bitmap; }
                }
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                while (baslerCanGrab)
                {
                    IGrabResult grabResult;
                    using (grabResult = camera1.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException))
                    {
                        if (grabResult.GrabSucceeded)
                        {
                            // 四通道RGBA
                            Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                            // 锁定位图的位
                            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                            // 将指针放置到位图的缓冲区
                            converter.OutputPixelFormat = PixelType.BGRA8packed;
                            IntPtr ptrBmp = bmpData.Scan0;
                            converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                            bitmap.UnlockBits(bmpData);
                            // 是否进行推理
                            if (isInference) { bitmap = Inference(bitmap); }
                            // 禁止跨线程直接访问控件，故invoke到主线程中
                            // 参考：https://bbs.csdn.net/topics/350050105
                            //       https://www.cnblogs.com/lky-learning/p/14025280.html
                            if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                            {
                                UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                                pictureBox1.BeginInvoke(update);
                            }
                            else { pictureBox1.Image = bitmap; }
                        }
                    }
                }
            }
            else {
                //usb相机
                // 是否进行推理
                //while (usbCanGrab)
                //{
                //    //IAsyncResult grabResult;
                //    //using (grabResult = usbDeviceSource.)

                //    // 获取当前每一帧的图像

                //    //BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                //    //bitmap.UnlockBits(bmpData);

                //}
            }
        }
        
        // 开始采集
        private void BnStartGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 标志位置位true
                    hikCanGrab = true;
                    // 开始采集
                    hIKVisionCamera.StartGrabbing();
                    // 用线程更新显示
                    hikGrabThread = new Thread(GrabThreadProcess);
                    hikGrabThread.Start();
                    // 控件操作
                    SetCtrlWhenStartGrab();
                }
                catch
                {
                    hikCanGrab = false;
                    hikGrabThread.Join();
                    MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 标志符号
                    baslerCanGrab = true;
                    // 开始Grab
                    baslerCamera.StartGrabbing();
                    // 用线程更新显示
                    baslerGrabThread = new Thread(GrabThreadProcess);
                    baslerGrabThread.Start();
                    // 控件操作
                    SetCtrlWhenStartGrab();
                }
                catch
                {
                    baslerCanGrab = false;
                    baslerGrabThread.Join();
                    MessageBox.Show("开始采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            } 
            else if (!chooseBasler && !chooseHIK && chooseUSB)
            {
                //USB开始采集
                try
                {
                    // 标志符号
                    usbCanGrab = true;
                    //// 开始Grab
                    usbDeviceSource.StartGrabbing();
                    //// 用线程更新显示
                    //usbGrabThread = new Thread(GrabThreadProcess);
                    //usbGrabThread.Start();
                    // 控件操作
                    SetCtrlWhenStartGrab();
                    bnLoadModel.Enabled = true;
                }
                catch
                {
                    usbCanGrab = false;
                    usbGrabThread.Join();
                    MessageBox.Show("开始采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        // 停止采集
        private void BnStopGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    hikCanGrab = false;   // 标志位设为false
                    hikGrabThread.Join();  // 主线程阻塞，等待线程结束
                    hIKVisionCamera.StopGrabbing();  // 停止采集
                    SetCtrlWhenStopGrab();  // 控件操作
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    baslerCanGrab = false;  // 标志位设为false
                    baslerGrabThread.Join();  // 主线程阻塞，等待线程结束
                    baslerCamera.StopGrabbing();  // 停止采集
                    SetCtrlWhenStopGrab();  // 控件操作
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            } 
            else if (chooseUSB && (!chooseBasler) && (!chooseHIK))
            {
                try
                {
                    usbCanGrab = false;  // 标志位设为false
                    //usbGrabThread.Join();  // 主线程阻塞，等待线程结束
                    usbDeviceSource.StopGrabbing();  // 停止采集
                    SetCtrlWhenStopGrab();  // 控件操作
                    isInference = false;
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        #region 参数设置
        private void BnGetParam_Click(object sender, EventArgs e)
        {
            // 参数
            string gain = null;  //增益值
            string exposure = null;  //暴光值
            if ((chooseHIK) && (!chooseBasler))
            {
                // 获取参数
                hIKVisionCamera.GetParam(ref gain, ref exposure, camera2);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 获取参数
                baslerCamera.GetParam(ref gain, ref exposure, camera1);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }
            else if (chooseUSB && (!chooseBasler) && (!chooseHIK))
            {
                int _exposure, _gain;
                this.usbDeviceSource.GetCameraProperty(CameraControlProperty.Exposure, out _exposure);
                this.usbDeviceSource.GetCameraProperty(CameraControlProperty.Zoom, out _gain);
                this.tbGain.Text = _gain.ToString();
                this.tbExposure.Text = _exposure.ToString();

            }
        }
        private void BnSetParam_Click(object sender, EventArgs e)
        {
            string gainshow = null;
            string exposureshow = null;
            if ((chooseHIK) && (!chooseBasler))
            {
                float exposure = float.Parse(tbExposure.Text);
                float gain = float.Parse(tbGain.Text);
                hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, camera2);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                long exposure = long.Parse(tbExposure.Text);
                long gain = long.Parse(tbGain.Text);
                baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, camera1);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            } else if (chooseUSB && (!chooseBasler) && (!chooseHIK))
            {
                this.camera3.DisplayPropertyPage(IntPtr.Zero);
                //bool a = this.usbDeviceSource.SetCameraProperty(
                // CameraControlProperty.Exposure, int.Parse(tbExposure.Text.Trim()),
                // CameraControlFlags.Manual);//曝光值
                //bool b = this.usbDeviceSource.videoSource.SetCameraProperty(
                // CameraControlProperty.Zoom, int.Parse(tbGain.Text.Trim()),
                // CameraControlFlags.Manual);
                //MessageBox.Show(a+" "+b);
                
            }
        }
        #endregion

        // 加载模型
        private void BnLoadModel_Click(object sender, EventArgs e)
        {

            BnStopDetection_Click(sender, e);
            //FolderBrowserDialog fileDialog = new FolderBrowserDialog();
            CommonOpenFileDialog fileDialog = new CommonOpenFileDialog();
            fileDialog.IsFolderPicker = true;
            //fileDialog.Description = "请选择模型路径";
            //fileDialog.ShowNewFolderButton = false;
            //if (modelPath != "")
            //{
            //    //fileDialog.file = modelPath;
            //}
            this.bnLoadModel.Enabled = false;
            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if(modelPath=="")
                    modelPath = fileDialog.FileName;
                //MessageBox.Show("已选择模型路径:" + modelPath, "选择文件提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    
                Func<bool> handle = () =>
                {
                    this.loadingCircle1.Invoke(
                        new Action(() => { this.loadingCircle1.Visible = true; }));
                    this.loadingCircle1.Invoke(
                         new Action(() => { this.loadingCircle1.Active = true; }));
                    model = CreatePaddlexModel(ref modelType, modelPath, useGPU, useTrt, useMkl, mklThreadNum, gpuID, key, useIrOptim);
                    return true;
                };
                
                IAsyncResult result = handle.BeginInvoke(null, null);
                Thread loading = new Thread(() =>
                {
                    while (!result.AsyncWaitHandle.WaitOne(500))
                    {
                           //do something else ...
                    }

                    if (handle.EndInvoke(result))
                    {
                        loadingCircle1.Active = false;
                        loadingCircle1.Visible = false;
                    }
                });
                loading.IsBackground = true ;
                loading.Start();
                //GC.KeepAlive(modelType);
                switch (modelType)
                    {
                        case 0: tbModeltype.Text = "0：图像分类"; break;
                        case 1: tbModeltype.Text = "1：目标检测"; break;
                        case 2: tbModeltype.Text = "2：语义分割"; break;
                    }

                    bnStartDetection.Enabled = true;
                    //bnStopDetection.Enabled = true;
                    bnThreshold.Enabled = true;
                }
            
            this.bnLoadModel.Enabled = true;

        }

        // 将Btimap类转换为byte[]类函数
        public unsafe static byte[] GetbyteData(Bitmap bmp)
        {
            BitmapData bmpData = null;
            bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int numbytes = bmpData.Stride * bmpData.Height;
            byte[] byteData = new byte[numbytes];
            IntPtr ptr = bmpData.Scan0;

            Marshal.Copy(ptr, byteData, 0, numbytes);

            return byteData;
        }

        public static IntPtr getBytesPtrInt(int[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        // 推理
        Bitmap resultShow;
        private Bitmap Inference(Bitmap bmp)
        {
            Bitmap bmpNew = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), bmp.PixelFormat);
            //Bitmap resultShow;
            Mat img = BitmapConverter.ToMat(bmpNew);

            int channel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            byte[] source = GetbyteData(bmp);
            if(modelType == 0)
            {
                bool res = PaddlexClsPredict(model, source, bmp.Height, bmp.Width, channel, out int categoryID, out float score);
                if(res)
                {
                    Scalar color = new Scalar(0, 0, 255); 
                    string text = category[categoryID] + ": " + score.ToString("f2");
                    OpenCvSharp.Size labelSize = Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, 1, 1, out int baseline);
                    Cv2.Rectangle(img, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(labelSize.Width + 60, labelSize.Height + 20), color, -1, LineTypes.AntiAlias, 0);
                    Cv2.PutText(img, text, new OpenCvSharp.Point(30, 30), HersheyFonts.HersheySimplex, 1, Scalar.White);
                }
            }
            else if(modelType == 1)
            {
                int max_box = 10;
                bool res = false;
                float[] result = new float[max_box * 6 +1];

                res = PaddlexDetPredict(model, source, bmp.Height, bmp.Width, channel, max_box, result, visualize);
                if (res)
                {
                    Scalar color = new Scalar(255, 0, 0);
                    for (int i = 0; i < result[0]; i++)
                    {
                        if (result[6 * i + 2] < 0.5)
                        {
                            continue;
                        }
                        Rect rect = new Rect((int)result[6 * i + 3], (int)result[6 * i + 4], (int)result[6 * i + 5], (int)result[6 * i + 6]);
                        Cv2.Rectangle(img, rect, color, 1, LineTypes.AntiAlias);
                        string text = category[(int)result[6 * i + 1]] + ":" + result[6 * i + 2].ToString("f2");
                        Cv2.PutText(img, text, new OpenCvSharp.Point((int)result[6 * i + 3], (int)result[6 * i + 4] - 5), HersheyFonts.HersheySimplex, 0.3, Scalar.Red);
                    }
                }
                else {
                    LogHelper.WriteLog("产品ID检测失败！");
                }
            }

            resultShow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format24bppRgb, img.Data);
            System.GC.Collect();
            return resultShow;
        }

        private void BnStartDetection_Click(object sender, EventArgs e)
        {
            bnLoadModel.Enabled = false;
            bnStopDetection.Enabled = true;
            bnStartDetection.Enabled = false;
            isInference = true;
        }

        // 停止检测
        private void BnStopDetection_Click(object sender, EventArgs e)
        {
            bnLoadModel.Enabled = true;
            bnStopDetection.Enabled = false;
            bnStartDetection.Enabled = true;
            isInference = false;
        }

        private void BnThreshold_Click(object sender, EventArgs e)
        {
            string path = modelPath + "/score_thresholds.yml";
            System.Diagnostics.Process.Start(path);
            ShowMessage("调整阈值后，请重新加载模型！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // 窗口关闭
        private void SingleCamera_FormClosing(object sender, FormClosingEventArgs e)
        {

            BnClose_Click(sender, e);
            System.Environment.Exit(0);
        }

        public void ShowMessage(string msg,string caption="",MessageBoxButtons btn = MessageBoxButtons.OK,MessageBoxIcon btnIcon = MessageBoxIcon.Warning)
        {
            this.BeginInvoke(new Action(()=> {
                MessageBox.Show(msg, caption, btn, btnIcon);
            }));
        }

        private void bnSaveImage_Click(object sender, EventArgs e)
        {
            this.bnSaveImage.Enabled = false;

            if (resultShow == null && pictureBox1.Image == null)
            {
                this.ShowMessage("保存产品图片失败！", "Warning");
                LogHelper.WriteLog("保存产品图片失败！");
                this.bnSaveImage.Enabled = true;
                return;
            }
            string filename = System.Guid.NewGuid().ToString("N");
            if (isInference && resultShow != null)
            {
                bool isExists = Learun.Util.DirFileHelper.IsExistDirectory(string.Format("{0}", ConfigurationManager.AppSettings["SaveImageDir"]));
                if (!isExists)
                    Learun.Util.DirFileHelper.CreateDir(string.Format("{0}", ConfigurationManager.AppSettings["SaveImageDir"]));
                resultShow.Save(
                        string.Format("{0}\\{1}.png", ConfigurationManager.AppSettings["SaveImageDir"], filename), System.Drawing.Imaging.ImageFormat.Png);
            }
            else
            {
                Bitmap tbmp = new Bitmap(pictureBox1.Image);
                tbmp.Save(string.Format("{0}\\{1}.png", ConfigurationManager.AppSettings["SaveImageDir"], filename), System.Drawing.Imaging.ImageFormat.Png);
            }
            this.ShowMessage("保存成功！", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.bnSaveImage.Enabled = true;
          
        }

    }
}
