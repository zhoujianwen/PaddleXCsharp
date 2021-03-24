using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace PaddleXCsharp
{
  
    public  class PaddleX
    {
        /* ================================= inference ================================= */
        #region 接口定义及参数
        public int modelType = 0;  // 模型的类型  0：分类模型；1：检测模型；2：分割模型
        public string modelPath = ""; // 模型目录路径
        public bool useGPU = true;  // 是否使用GPU
        public bool useTrt = false;  // 是否使用TensorRT
        public bool useMkl = true;  // 是否使用MKLDNN加速模型在CPU上的预测性能
        public int mklThreadNum = 8; // 使用MKLDNN时，线程数量
        public int gpuID = 0; // 使用GPU的ID号
        public string key = ""; //模型解密密钥，此参数用于加载加密的PaddleX模型时使用
        public bool useIrOptim = false; // 是否加速模型后进行图优化
        public bool visualize = false;
        public bool isInference = false;
        public IntPtr model; // 模型

        // 目标物种类，需根据实际情况修改！
        public string[] category = { "bocai",
                                    "changqiezi",
                                    "hongxiancai"
                                    ,"huluobo",
                                    "xihongshi",
                                    "xilanhua"
                                     };

        // 定义CreatePaddlexModel接口
        [DllImport("paddlex_inference.dll", EntryPoint = "CreatePaddlexModel", CharSet = CharSet.Ansi)]
        public static extern IntPtr CreatePaddlexModel(ref int modelType,
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
        public static extern bool PaddlexClsPredict(IntPtr model, byte[] image, int height, int width, int channels, out int categoryID, out float score);

        // 定义检测接口
        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexDetPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PaddlexDetPredict(IntPtr model, byte[] image, int height, int width, int channels, int max_box, float[] result, bool visualize);
        #endregion

        // 定义语义分割接口
        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexSegPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PaddlexSegPredict(IntPtr model, byte[] image, int height, int width, int channels, long[] label_map, float[] score_map, bool visualize);

        // 定义释放模型内存接口
        [DllImport("detector.dll", EntryPoint = "FreeNewMemory", CharSet = CharSet.Ansi)]
        public static extern void FreeModelMemory(IntPtr model);

    }
}
