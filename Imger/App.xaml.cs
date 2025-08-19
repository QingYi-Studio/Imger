using System;
using System.IO;
using System.Runtime;
using System.Windows;

namespace Imger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            if (File.Exists("USE_INDEPENDENT_GPU"))
            {
                GpuEnforcer.ForceDedicatedGpu();
            }

            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Temp"));
            ProfileOptimization.SetProfileRoot(Path.Combine(AppContext.BaseDirectory, "Temp"));
            ProfileOptimization.StartProfile(".profile");
        }
    }

}
