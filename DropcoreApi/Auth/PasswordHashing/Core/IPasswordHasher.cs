public interface IPasswordHasher
{
    PasswordHash Hash(Password password);
}