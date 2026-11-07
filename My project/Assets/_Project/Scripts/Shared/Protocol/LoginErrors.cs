namespace Game.Shared.Protocol
{
    /// <summary>Localization keys of login refusals (module 13), sent in Welcome.Error.</summary>
    public static class LoginErrors
    {
        public const string BadLogin = "net.bad_login";
        public const string WrongPassword = "net.wrong_password";
        public const string BadName = "net.bad_name";
        public const string NameTaken = "net.name_taken";
        /// <summary>The account logged in again elsewhere: the old session is closed (its character is saved first).</summary>
        public const string LoggedElsewhere = "net.logged_elsewhere";
        public const string TokenExpired = "net.token_expired";
    }
}
