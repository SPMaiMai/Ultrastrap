using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for LaunchPage.xaml
    /// </summary>
    public partial class LaunchPage
    {
        public LaunchPage()
        {
            DataContext = new LaunchViewModel();
            InitializeComponent();
        }
    }
}
