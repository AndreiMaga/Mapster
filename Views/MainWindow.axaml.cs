using Avalonia.ReactiveUI;
using Mapster.ViewModels;

namespace Mapster.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
