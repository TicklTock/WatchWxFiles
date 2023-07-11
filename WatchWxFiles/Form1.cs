using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WatchWxFiles.Properties;

namespace WatchWxFiles
{
    public partial class Form1 : Form
    {
        //当前程序路径
        private static string exePath = Application.ExecutablePath;
        //当前程序名称
        private static string exeName = Path.GetFileName(exePath);
        //创建/打开配置文件
        private Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        //通过环境变量读取用户路径
        private static string userPath = Environment.GetEnvironmentVariable("USERPROFILE");
        //微信文件默认位置
        private string wxFilePath = $"{userPath}\\Documents\\WeChat Files";
        //微信文件子目录，用于判断目录文件
        private static string wxSubPath = "FileStorage\\File";
        //初始化一个文件监控类
        private FileSystemWatcher watcher = new FileSystemWatcher();
        public Form1()
        {
            
            InitializeComponent();
            init();
            


        }
        /*初始化*/
        private void init() {
            
            /*添加配置文件，用于自启后自动进行监控*/
            var settings = configFile.AppSettings.Settings;
            if (settings["restart"] == null)
            {
                settings.Add("restart", "false");
            }
            else if (settings["restart"].Value == "true")
            {
                checkBox_login.Checked = true;
                button_run_Click(null, null);
            }
            else
            {
                checkBox_login.Checked = false;
            }

            if (settings["wxFilePath"] == null)
            {
                settings.Add("wxFilePath", wxFilePath);
            }
            else
            {
                wxFilePath = settings["wxFilePath"].Value;
            }

            /*watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;*/
            //只监控文件属性变化
            watcher.NotifyFilter = NotifyFilters.Attributes;
            /*watcher.Created += OnCreated;*/
            //只监控文件发生改变时发生的动作
            watcher.Changed += OnChanged;
            /*watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;*/
            watcher.IncludeSubdirectories = true;
            toolStripStatusLabel1.Text = $"当前监测路径:{wxFilePath}";
        }
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            //监控到没有包含微信文件子目录的文件属性更改情况下不进行修改
            if (!e.FullPath.Contains(wxSubPath))
            {
                return;
            }
            Console.WriteLine($"Changed: {e.FullPath}");
            FileInfo fileInfo = new FileInfo(e.FullPath);
            //检测到文件是只读属性则修改为非只读文件
            if(fileInfo.IsReadOnly == true)
            {
                fileInfo.Attributes = FileAttributes.Normal;
            }
            Console.WriteLine(fileInfo.IsReadOnly);
        }
        /*开始/结束监控*/
        private void button_run_Click(object sender, EventArgs e)
        {
            watcher.Path = wxFilePath;
            watcher.EnableRaisingEvents = !watcher.EnableRaisingEvents;
            if (watcher.EnableRaisingEvents)
            {
                button_run.Text = "关闭监控";
                toolStripStatusLabel1.Text = $"开始监控:{wxFilePath}";
                button_reset.Enabled = false;
            }
            else
            {
                button_run.Text = "开始监控";
                toolStripStatusLabel1.Text = "已关闭监控!";
                button_reset.Enabled = true;
                Thread thread = new Thread(() =>
                {
                    Thread.Sleep(2000);
                    toolStripStatusLabel1.Text = $"当前监测路径:{wxFilePath}";
                });
                thread.Start();
                
            }
            

        }
        /*修改微信文件路径*/
        private void button_reset_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog(this)== DialogResult.OK)
            {
                string customPath = folderBrowserDialog1.SelectedPath;
                bool isWxPath = false;
                foreach(var path in Directory.GetDirectories(customPath))
                {
                    if (path.Contains("wxid"))
                    {
                        isWxPath = true;
                        break;
                    }
                };
                if (!isWxPath)
                {
                    MessageBox.Show("目录选择可能错误或没有个人资料文件夹!");
                    return;
                }
                wxFilePath = folderBrowserDialog1.SelectedPath;
                var settings = configFile.AppSettings.Settings;
                settings["wxFilePath"].Value = wxFilePath;
                configFile.Save();
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                toolStripStatusLabel1.Text = toolStripStatusLabel1.Text = $"当前监测路径:{wxFilePath}";


            }

        }
        /*随开机启动*/
        private void checkBox_login_CheckedChanged(object sender, EventArgs e)
        {
            var settings = configFile.AppSettings.Settings;
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (checkBox_login.Checked)
            {

                if (key != null)
                {
                    key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                }
                key.SetValue(exeName, exePath);
                settings["restart"].Value =  "true";
            }
            else {
                key.DeleteValue(exeName);
                settings["restart"].Value = "false";
            }

            configFile.Save();
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
        /*重写关闭时事件*/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //开启监控时关闭软件会最小化到托盘
            if (button_run.Text == "关闭监控")
            {
                
                e.Cancel = true;
                this.Visible = false;
                notifyIcon1.Visible = true;
                MessageBox.Show("已最小化到托盘,双击托盘图标可重新打开!");
            }
            //关闭监控时关闭软件会退出软件
            else
            {
                退出ToolStripMenuItem_Click(null,null);
            }
            
        }
        //双击托盘图标打开软件
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Visible = true;
            this.Focus();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1_DoubleClick(sender, e);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if(WindowState == FormWindowState.Minimized)
            {
                if (button_run.Text == "关闭监控")
                {
                    MessageBox.Show("已最小化到托盘,双击托盘图标可重新打开!");
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = false;
                    notifyIcon1.Visible = true;
                }
            }
        }
    }

    
}
