using ChatManager.Models;
using System;

public class UserFriendships
{
    public int Id { get; set; }
    public int UserID { get; set; }
    public int FriendId { get; set; }
    public bool IsFriend { get; set; }
    public bool isPending { get; set; }
    public bool isDeclined { get; set; }
}