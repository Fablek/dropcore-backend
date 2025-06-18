namespace UserService.DTOs;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public long UsedSpace { get; set; } = 0;
    public long SpaceLimit { get; set; } = 1073741824; // 1 GB 
}
