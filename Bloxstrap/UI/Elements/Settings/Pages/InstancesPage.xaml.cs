using System.Windows.Threading;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for InstancesPage.xaml
    /// </summary>
    public partial class InstancesPage
    {
        private readonly DispatcherTimer _timer;

        public InstancesPage()
        {
            var viewModel = new InstancesViewModel();
            DataContext = viewModel;
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += (_, _) => viewModel.Refresh();
            Loaded += (_, _) => _timer.Start();
            Unloaded += (_, _) => _timer.Stop();
        }
    }
}
