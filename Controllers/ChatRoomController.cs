using ChatManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer.Symbols;

namespace ChatManager.Controllers
{
    public class ChatRoomController : Controller
    {
        // GET: ChatRoom
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetFriendList(bool forceRefresh = false)
        {
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged || DB.UserFriendships.HasChanged)
            {
                IEnumerable<User> users = new List<User>();
                users = DB.Users.SortedUsers();
                var myFriends = users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && uf.IsFriend) && !u.Blocked);

                if(myFriends.Count() > 0) {
                    return PartialView(myFriends);
                }
                return null;
            }
            return null;
        }
    }
}