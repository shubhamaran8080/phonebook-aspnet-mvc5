namespace Phonebook.Web.Services
{
    /// <summary>
    /// BCrypt password hashing. Compatible with hashes created by the
    /// source .NET 10 application's BCrypt.Net-Next library.
    /// </summary>
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool Verify(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
