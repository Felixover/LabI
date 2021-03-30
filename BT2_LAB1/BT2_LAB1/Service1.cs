using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BT2_LAB1
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        Timer timerr = new Timer();
        Timer timers = new Timer();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Code for starting the service
            WriteToFile("Service started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime_Check);
            timer.Interval = 5000;
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            //
        }

        protected void OnElapsedTime_Check(Object source, ElapsedEventArgs e)
        {
            Check(source,e);
        }

        public void Check(Object source, ElapsedEventArgs e)
        {
            //get the numbers of Notepad process is running
            Process[] processes = null;
            processes = Process.GetProcessesByName("notepad");
            //if the Notepad processes list is empty, start a new notepad process
            if (processes.Length == 0)
            {
                Process.Start("Notepad.exe");
                WriteToFile("Started process at" + DateTime.Now);
            }
            else
            {
                //if the list has any processes, kill all of them
                //so if you open the service, all of running notepad processes will be killed
                foreach (Process process in processes)
                {
                    process.Kill();
                    WriteToFile("Stopped process at" + DateTime.Now);
                }
            }

        }

        public void Schedule_Start()
        {
            //function to schedule the service if you want
            ServiceController service = new ServiceController("UITService.Demo");

            if ((service.Status.Equals(ServiceControllerStatus.Stopped)) ||

                (service.Status.Equals(ServiceControllerStatus.StopPending)))
            {
                service.Start();
            }
        }

        public void Schedule_Stop()
        {
            //function to schedule the service if you want
            ServiceController service = new ServiceController("UITService.Demo");

            if ((service.Status.Equals(ServiceControllerStatus.Running)) ||

                (service.Status.Equals(ServiceControllerStatus.Paused)))

                service.Stop();
        }

        public void WriteToFile(string Message)
        {

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') +
                ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
