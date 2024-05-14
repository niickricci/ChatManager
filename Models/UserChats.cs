using ChatManager.Models;
using System;
using System.Collections.Generic;
using System.Web.Services.Description;

public class UserChats 
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int FriendId { get; set; }
    public string Message { get; set; }
    public DateTime Date { get; set; }
    public bool isModified { get; set; }
}