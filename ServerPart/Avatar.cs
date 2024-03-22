/// <summary>
/// An implementation of profile image
/// </summary>
class Avatar
{
    public string AvatarDateTime { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public byte[] Bytes { get; set; } = new byte[1024];
}