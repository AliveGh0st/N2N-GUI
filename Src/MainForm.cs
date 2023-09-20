using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace test
{
    public partial class Form1 : Form
    {
        const bool CONNECTED = true;
        const bool NOT_CONNECTED = false;
        // 定义一个字符串构建器对象，用于存储进程的输出
        public StringBuilder output = new StringBuilder();
        public static Process process;
        public static System.Threading.Timer updateStatus;

        public ToolTip toolTip1 = new ToolTip(); // 创建一个 ToolTip 控件

        // 当前连接使用的网卡
        public NetworkInterface useNetworkCard = null;

        // 连接状态
        public BoolVariable connectSta; 

        public Form1()
        {
            InitializeComponent();

            // 定时器任务，更新网卡状态
            updateStatus = new System.Threading.Timer(TimerCallback, null, 0, 100);

            // 更新连接状态
            connectSta = new BoolVariable();
            connectSta.ValueChanged += new EventHandler<BoolEventArgs>(connectSta_ValueChanged);
            //connectSta.Value = ;

            if (connectSta.Value == CONNECTED)
            {
                button1.Enabled = false;
                button1.Text = "已连接";
                button2.Enabled = true;
                button2.Text = "断开";
            }
            else
            {
                button1.Enabled = true;
                button1.Text = "连接";
                button2.Enabled = false;
                button2.Text = "未连接";
            }

            textBox1.Text = "n2nGUI(Version 0.0.1) and n2nEdge(Version 3.1.1-16-g23e168b-dirty-r1200) is ok.";


        }

        private void button1_Click(object sender, EventArgs e)
        {
            process = new Process();
            // 创建一个进程对象，并设置其启动信息
            process.StartInfo.FileName = ".\\edge.exe";
            process.StartInfo.Arguments = "-l hz1.squad.ink:9527 -c Communit1 -E -S1  -x 1";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.Verb = "runas";

            // 为进程添加OutputDataReceived事件处理程序
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            // 启动进程，并获取其输出
            process.Start();
            process.BeginOutputReadLine();
        }

        // 定义一个事件处理程序，用于异步读取进程的输出
        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            
            // 如果输出不为空，则追加到字符串构建器中，并更新文本框的内容
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                output.Append(outLine.Data + Environment.NewLine);

                // 使用Invoke方法来修改文本框的Text属性
                textBox1.Invoke(new Action(() => 
                {
                    textBox1.Text = output.ToString(); 
                }));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process[] procs = Process.GetProcessesByName("edge");
            foreach (Process proc in procs)
            {
                proc.Kill();
                textBox1.AppendText("节点已断开。");
            }
        }

        private void ShowToolTip(string text)
        {
            // 判断是否需要调用 UI 线程
            if (label2.InvokeRequired)
            {
                // 创建一个委托并传入参数
                Action<string> action = new Action<string>(ShowToolTip);
                // 使用 Invoke 或 BeginInvoke 方法在 UI 线程上执行委托
                label2.Invoke(action, text);
            }
            else
            {
                // 在 UI 线程上直接调用 tooltip 的方法
                toolTip1.SetToolTip(label2, text);
            }
        }

        private void connectSta_ValueChanged(object sender, BoolEventArgs e)    // 连接状态改变事件
        {
            if (connectSta.Value == CONNECTED)
            {
                IPInterfaceProperties ipProps = useNetworkCard.GetIPProperties(); // 获取 IP 属性
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses) // 遍历每个单播地址
                {
                    label2.Invoke(new Action(() =>
                    {
                        label2.Text = addr.Address.ToString(); // 输出地址
                        label2.Font = new Font(label2.Font, FontStyle.Underline);
                        ShowToolTip("点击复制");
                    }));
                    
                }

                // 输出连接状态
                button1.Invoke(new Action(() => {
                    button1.Text = "已连接";
                    button1.Enabled = false;
                }));
                    
                
                button2.Invoke(new Action(() =>
                {
                    button2.Enabled = true;
                    button2.Text = "断开";
                }));
                Thread.Sleep(2000);
                textBox1.Invoke(new Action(() =>
                {
                    output.Append("节点已连接。" + Environment.NewLine);
                    textBox1.Text = output.ToString();
                }));
                
                
            }
            else
            {
                // 输出断开状态
                label2.Invoke(new Action(() =>
                {
                    label2.Text = "未连接";
                }));
                button1.Invoke(new Action(() =>
                {
                    button1.Text = "连接";
                    button1.Enabled = true;
                }));
                button2.Invoke(new Action(() =>
                {
                    button2.Enabled = false;
                    button2.Text = "未连接";
                }));
                label2.Invoke(new Action(() =>
                {
                    label2.Text = "未连接";
                    label2.Font = new Font(label2.Font, FontStyle.Regular);
                }));

                ShowToolTip("");
                
            }
        }
        
        private void TimerCallback(object state) 
        {
            // 获取所有网络接口
            NetworkInterface[] networkCards = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkCard in networkCards)
            {
                // 获取网络接口的描述信息
                string description = networkCard.Description;
                
                // 判断是否包含tap或者n2n等关键字
                if (description.Contains("TAP"))
                {
                    // 输出网络接口的名称和描述信息
                    //Console.WriteLine("Name: " + networkCard.Name);
                    //Console.WriteLine("Description: " + description);

                    useNetworkCard = networkCard;
                }
            }

            // 获取网络接口的运行状态
            OperationalStatus status = useNetworkCard.OperationalStatus;
            // 判断是否等于OperationalStatus.Up
            if (status == OperationalStatus.Up)
            {
                connectSta.Value = CONNECTED;   // 已连接
            }
            else
            {
                connectSta.Value = NOT_CONNECTED;   // 未连接
                useNetworkCard = null;      // 清除当前使用的网卡
            }
        }

        // 滚动条移动到最下方
        private void OnTextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret ();
        }

        // 移除TextBox焦点
        private void Form1_Activated(object sender, EventArgs e)
        {
            button1.Focus();
        }

        // 关闭时kill edge
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Process[] procs = Process.GetProcessesByName("edge");
            foreach (Process proc in procs)
            {
                proc.Kill();
                this.textBox1.AppendText("节点已断开。");
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            string text = label2.Text; // 获取 LinkLabel 中的文本
            Clipboard.SetDataObject(text); // 将文本复制到剪贴板
            ShowToolTip("已复制"); // 为 label1 控件设置帮助文本
        }

        private void label2_MouseLeave(object sender, EventArgs e)
        {
            if (connectSta.Value == CONNECTED)
            {
                ShowToolTip("点击复制");
            }
        }
    }

    public class BoolEventArgs : EventArgs // 定义一个继承自 EventArgs 的类，用于封装 bool 变量的值
    {
        public bool Value { get; set; } // 定义一个属性，表示 bool 变量的值
        public BoolEventArgs(bool value) // 定义一个构造函数，接收 bool 变量的值
        {
            Value = value; // 赋值
        }
    }

    public class BoolVariable // 定义一个封装 bool 变量的类
    {
        public event EventHandler<BoolEventArgs> ValueChanged; // 定义一个事件，表示 bool 变量的值发生改变时触发

        private bool value; // 定义一个私有字段，表示 bool 变量的值
        public bool Value // 定义一个公有属性，用于访问和修改 bool 变量的值
        {
            get { return value; } // 获取值
            set
            {
                if (this.value != value) // 比较新旧值 
                {
                    this.value = value; // 修改值
                    OnValueChanged(new BoolEventArgs(value)); // 调用事件触发函数
                }
            }
        }

        protected virtual void OnValueChanged(BoolEventArgs e) // 定义一个虚拟函数，用于触发事件
        {
            if (ValueChanged != null)
            {
                ValueChanged.Invoke(this, e);
            }
        }
    }

}
