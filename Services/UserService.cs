namespace eMarket.Services
{
    public static class UserService
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public static string? ValidatePassword(string password)
        {
            if (password.Length < 8)
                return "Password must be at least 8 characters long.";

            if (!password.Any(char.IsUpper))
                return "Password must include an uppercase letter.";

            if (!password.Any(char.IsLower))
                return "Password must include a lowercase letter.";

            if (!password.Any(char.IsDigit))
                return "Password must include a number.";
            return null;
        }
    }
}
