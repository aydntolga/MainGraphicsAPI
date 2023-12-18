using System;
using System.Diagnostics;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ApiService
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

                // Projeler başlatıldıktan sonra Swagger sayfalarını tarayıcıda aç
                await OpenSwaggerInBrowser(5205); // Projelerin portlarına göre güncelleyin
                await OpenSwaggerInBrowser(5036);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
                // Hizmeti durdurmak yerine hata mesajını kaydetmeyi veya uyarı göndermeyi değerlendirin.
                //StopService();
            }
        }

        private async Task StartProjectAsync(string projectPath, int port)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "iisexpress.exe";
                process.StartInfo.Arguments = $"/path:\"{projectPath}\" /port:{port}";
                process.Start();

                // Projeyi başlatıldıktan sonra IIS sinyalini bekleyin.
                await Task.Run(() => process.WaitForExit(Timeout.Infinite));

                if (!process.HasExited)
                {
                    Console.WriteLine("Proje başlatılamadı.");
                    // Hata durumunda projeyi durdurun.
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        private async Task OpenSwaggerInBrowser(int port)
        {
            string swaggerUrl = $"http://localhost:{port}/swagger/index.html";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(swaggerUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Swagger sayfası başarıyla açıldı: {swaggerUrl}");
                    }
                    else
                    {
                        Console.WriteLine($"Swagger sayfası açılamadı. Hata kodu: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Swagger sayfasını açarken bir hata oluştu: {ex.Message}");
                }
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
