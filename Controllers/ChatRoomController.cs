using ChatManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChatManager.Controllers
{
    public class ChatRoomController : Controller
    {
        // GET: ChatRoom
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetMessages(bool forceRefresh = false, string[] filterType = null, string keyword = "")
        {
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged || DB.UserFriendships.HasChanged)
            {
                IEnumerable<User> users = new List<User>();

                if (filterType != null && filterType.Length > 0)
                {
                    users = DB.Users.SortedUsers();
                    List<User> filteredUsers = new List<User>();

                    foreach (var type in filterType)
                    {
                        switch (type)
                        {
                            case "myfriends":
                                filteredUsers.AddRange(users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && uf.IsFriend) && !u.Blocked).Where(u => u.GetFullName().ToLower().Contains(keyword.ToLower())));
                                break;
                            case "notfriends":
                                filteredUsers.AddRange(users.Where(u => !DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && uf.IsFriend) && !DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && !uf.isPending) && !DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && !uf.isDeclined) && !u.Blocked).Where(u => u.GetFullName().ToLower().Contains(keyword.ToLower())));
                                break;
                            case "requests":
                                filteredUsers.AddRange(users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && !uf.IsFriend && uf.isPending) && !u.Blocked).Where(u => u.GetFullName().ToLower().Contains(keyword.ToLower())));
                                break;
                            case "pending":
                                filteredUsers.AddRange(users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.FriendId == u.Id && uf.UserID == OnlineUsers.GetSessionUser().Id && !uf.IsFriend && uf.isPending) && !u.Blocked).Where(u => u.GetFullName().ToLower().Contains(keyword.ToLower())));
                                break;
                            case "declined":
                                filteredUsers.AddRange(users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.FriendId == u.Id && uf.UserID == OnlineUsers.GetSessionUser().Id && !uf.IsFriend && uf.isDeclined) && !u.Blocked).Where(u => u.GetFullName().ToLower().Contains(keyword.ToLower())));
                                break;
                            case "blocked":
                                filteredUsers.AddRange(users.Where(u => u.Blocked).Where(u => u.GetFullName().ToLower().Contains(keyword.ToLower())));
                                break;
                        }
                    }
                    users = filteredUsers;
                }
                return PartialView(users);
            }

            return null;
        }
    }
}