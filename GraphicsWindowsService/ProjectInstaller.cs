using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Management;

namespace GraphicsWindowsService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private readonly ServiceProcessInstaller serviceProcessInstaller;
        private readonly ServiceInstaller serviceInstaller;
        public ProjectInstaller()
        {
            InitializeComponent();

            this.serviceProcessInstaller = new ServiceProcessInstaller();
            this.serviceInstaller1 = new ServiceInstaller();

            this.serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;


            this.serviceInstaller1.Description = "API TEST ";
            this.serviceInstaller1.DisplayName = "TOLGA";
            this.serviceInstaller1.ServiceName = "Service2";
            this.serviceInstaller1.StartType = ServiceStartMode.Automatic;

            this.Installers.AddRange(new Installer[]
                {
                    this.serviceProcessInstaller,
                    this.serviceInstaller1
                });
        }
    }
} 