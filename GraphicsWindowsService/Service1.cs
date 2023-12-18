using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace GraphicsWindowsService
{
    public partial class Service1 : ServiceBase
    {
        private Process process1;
        private Process process2;

        public Service1()
        {
            InitializeComponent();
        }


        protected override async void OnStart(string[] args)
        {
            try
            {
                string projectPath1 = @"D:\MainGraphicsAPI\GratisGraphicsAPI\GratisGraphicsAPI.csproj";
                string projectPath2 = @"D:\MainGraphicsAPI\MainGraphicsAPI\MainGraphicsAPI.csproj";

                await StartProjectAsync(projectPath1, 5205);
                await StartProjectAsync(projectPath2, 5036);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }

        private async Task StartProjectAsync(string projectPath, int port)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "iisexpress.exe";
                process.StartInfo.Arguments = $"/path:\"{projectPath}\" /port:{port}";
                process.Start();

                // Projelerin başlamasını beklemek için Task.Delay kullanın.
                await Task.Delay(TimeSpan.FromSeconds(30)); // Örnek olarak 30 saniye bekleyin, süreyi ayarlayabilirsiniz.
            }
        }


        protected override void OnStop()
        {
            StopProject(process1);
            StopProject(process2);
        }

        private void StopProject(Process process)
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

    }
}
