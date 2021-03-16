using System;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Orleans;

namespace Client.Xamarin.ViewModels
{
    public class CreateChatPageViewModel : BaseViewModel
    {
        private readonly IClusterClient _clusterClient;
        
        private string _name;
        private bool _isPrivate;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public bool IsPrivate
        {
            get => _isPrivate;
            set
            {
                _isPrivate = value;
                OnPropertyChanged();
            }
        }

        public CreateChatPageViewModel(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public async Task<Guid> CreateAsync()
        {
            var chat = _clusterClient.GetGrain<IChat>(Guid.NewGuid());
            await chat.CreateAsync(new ChatSettingsModel
            {
                Name = Name,
                IsPrivate = IsPrivate,
                OwnerNickName = LocalStore.GetUserNickName()
            });

            var user = LocalStore.GetUserGrain();
            await chat.JoinAsync(user);
            
            return chat.GetPrimaryKey();
        }
    }
}