using ReactiveUI;
using System.Reactive;

namespace Mapster.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IScreen
    {

        public RoutingState Router { get; } = new RoutingState();

        public ReactiveCommand<Unit, IRoutableViewModel>? GoNext { get; }

        public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;


        public MainWindowViewModel()
        {
            Router.Navigate.Execute(new LoadingScreenViewModel(this));
        }



    }
}
