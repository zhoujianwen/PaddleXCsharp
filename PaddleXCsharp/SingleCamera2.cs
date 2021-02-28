using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;

namespace PaddleXCsharp
{
    public partial class SingleCamera2 :UIAsideHeaderMainFrame
    {
        public SingleCamera2()
        {
            InitializeComponent();
            this.uiTitlePanel1.TitleColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(76)))), ((int)(((byte)(76)))));
            this.uiTitlePanel2.TitleColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(76)))), ((int)(((byte)(76)))));
            this.uiTitlePanel3.TitleColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(76)))), ((int)(((byte)(76)))));

            AddPage(new CameraWindows(), 1001);
        }

        private void SingleCamera2_Load(object sender, EventArgs e)
        {

        }
    }
}
