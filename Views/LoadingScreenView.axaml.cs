using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Mapster.ViewModels;

namespace Mapster.Views
{
    public partial class LoadingScreenView : ReactiveUserControl<LoadingScreenViewModel>
    {

        public LoadingScreenView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


    }
}
