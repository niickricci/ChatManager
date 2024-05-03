using ChatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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

        public ActionResult GetFriendsList(bool forceRefresh = false)
        {
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged || DB.UserFriendships.HasChanged)
            {
                IEnumerable<User> users = new List<User>();
                users = DB.Users.SortedUsers();
                var myFriends = users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && uf.IsFriend) && !u.Blocked);

                if (myFriends.Count() > 0)
                {
                    return PartialView(myFriends);
                }
                return null;
            }
            return null;
        }

        public JsonResult SetCurrentTarget(int userId)
        {
            if (DB.Users.Get(userId) != null)
            {
                Session["currentChatTarget"] = userId;

                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetChatRoom(bool forceRefresh = false)
        {
            if (Session["currentChatTarget"] == null)
            {
                return null;
            }

            int userId = OnlineUsers.GetSessionUser().Id;
            int friendId = (int)Session["currentChatTarget"];
            //User friend = DB.Users.Get(friendId);

            if (DB.Users.Get(userId) == null)
            {
                return null;
            }


            var currentUserMessages = DB.Users.GetUsersChats(userId, friendId);
            var friendMessages = DB.Users.GetUsersChats(friendId, userId);
            
            return PartialView((currentUserMessages, friendMessages));
        }
    }
}