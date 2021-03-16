using System;
using GrainInterfaces;
using Plugin.Settings;

namespace Client.Xamarin
{
    public static class LocalStore
    {
        private const string UserNickName = "userNickName";
        private const string UserId = "userId";
        
        private static IUser _user;

        public static Guid GetUserId()
        {
            return CrossSettings.Current.GetValueOrDefault<Guid>(UserId);
        }
        
        public static string GetUserNickName()
        {
            return CrossSettings.Current.GetValueOrDefault<string>(UserNickName);
        }

        public static void SetNickName(string nickName)
        {
            CrossSettings.Current.AddOrUpdateValue(UserNickName, nickName);
        }

        public static void SetUserId(Guid userId)
        {
            CrossSettings.Current.AddOrUpdateValue(UserId, userId);
        }

        public static bool IsLoggedIn()
        {
            return CrossSettings.Current.Contains(UserNickName) && CrossSettings.Current.Contains(UserId);
        }

        public static IUser GetUserGrain()
        {
            return _user;
        }

        public static void SetUserGrain(IUser user)
        {
            _user = user;
        }
    }
}