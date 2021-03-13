using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using Plugin.Settings;

namespace Client.Xamarin.ViewModels
{
    public class LoginPageViewModel : BaseViewModel
    {
        private readonly IClusterClient _clusterClient;
        private string _userName;

        [Required(ErrorMessage = "Name cannot be empty!")]
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        public LoginPageViewModel(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public async Task LoginAsync()
        {
            var user = _clusterClient.GetGrain<IUser>(UserName);
            var nickName = user.GetPrimaryKeyString();

            LocalStore.SetNickName(nickName);
            LocalStore.SetUserId(await user.GetUserIdAsync());
        }
    }
}