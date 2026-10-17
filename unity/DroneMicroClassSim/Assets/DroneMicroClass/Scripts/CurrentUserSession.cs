namespace DroneMicroClass
{
    public static class CurrentUserSession
    {
        public static LocalUserDatabase.UserRecord CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;

        public static void SetUser(LocalUserDatabase.UserRecord user)
        {
            CurrentUser = user;
        }

        public static void Clear()
        {
            CurrentUser = null;
        }

        public static LocalUserDatabase.UserRecord GetUserOrDefault()
        {
            if (CurrentUser == null)
            {
                CurrentUser = LocalUserDatabase.GetDefaultPilotUser();
            }

            return CurrentUser;
        }
    }
}
