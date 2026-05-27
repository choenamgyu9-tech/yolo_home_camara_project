using System.Windows;
using yolo_home_camera_project.Data;

namespace yolo_home_camera_project
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDbContext dbContext = new();
            await dbContext.InitializeDatabaseAsync();
        }
    }
}
