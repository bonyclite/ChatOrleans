using System.Windows.Input;

namespace Client.Xamarin.ViewModels
{
    public abstract class RefreshViewModel : BaseViewModel
    {
        private bool _isRefreshing;
        
        public abstract ICommand RefreshCommand { get; }
        
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
    }
}