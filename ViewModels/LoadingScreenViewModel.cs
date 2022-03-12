using Avalonia.Threading;
using Mapster.Protobuff;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace Mapster.ViewModels
{
    public class LoadingScreenViewModel : ViewModelBase, IRoutableViewModel
    {
        #region IRoutableViewModel
        public IScreen HostScreen { get; }
        public ReactiveCommand<Unit, Unit> GoBack => HostScreen.Router.NavigateBack;
        public bool IsNavigatedTo { get; set; }
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString();

        #endregion

        private double _progressValue = 0;
        public double ProgressValue
        {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);

        }
        public LoadingScreenViewModel(IScreen screen)
        {
            HostScreen = screen;

            ReadFromProto.Instance.SetValueEvent += SetValue;
            ReadFromProto.Instance.FinishedEvent += FinishedEvent;


            this.WhenNavigatedToObservable().Subscribe(_ =>
            {
                IsNavigatedTo = true;
                Task task = Task.Run(async () =>
                {
                    await ReadFromProto.Instance.LoadProtos();
                });
            });

            this.WhenNavigatingFromObservable().Subscribe(_ =>
            {
                IsNavigatedTo = false;
            });

        }

        #region ReadFromOsm Events
        public void SetValue(double value) => ProgressValue = value;
        private void FinishedEvent()
        {
            if (IsNavigatedTo)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    GoBack.Execute();
                });
            }
        }

        #endregion


    }
}
