using System;

public class UsersLogsJournal
{
    public int Id { get; set; }
    public int UserID { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LogoutTime { get; set; }
}