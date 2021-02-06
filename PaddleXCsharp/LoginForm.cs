using PaddleXCsharp.User;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;

namespace PaddleXCsharp
{
    public partial class LoginForm : Form
    {
        //public static Dictionary<string, Form> listForm = new Dictionary<string, Form>();
        public LoginForm()
        {
            InitializeComponent();
            //this.textBox1.AutoSize = false;
            //this.textBox1.Height = 30;
            //this.textBox2.AutoSize = false;
            //this.textBox2.Height = 30;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                this.Hide();
                SingleCamera singleCamera = new SingleCamera();
                //listForm.Add("SingleCamera",singleCamera);
                User.UserService test = new User.UserService();
                UserEntity user = test.CheckLogin(this.skinTextBox1.Text, this.skinTextBox1.Text);
                if (user.LoginOk)
                {
                    singleCamera.Show();
                }
                else {
                    if (MessageBox.Show(user.LoginMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        this.Show();
                    }
                }
            }
            else if (radioButton2.Checked)
            {
                this.Hide();
                DoubleCamera doubleCamera = new DoubleCamera();
                //listForm.Add("DoubleCamera",doubleCamera);
                doubleCamera.Show();
            }
            else if (radioButton3.Checked)
            {
                MessageBox.Show("还没做呢！");
            }
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
