﻿using ChatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer.Symbols;
using System.Web.Services.Description;

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
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged || DB.UserFriendships.HasChanged || DB.UserChats.HasChanged)
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
            return null;
        }
        [HttpPost]
        public ActionResult SendMessage(string message)
        {
            int friendId = (int)Session["currentChatTarget"];
            int userId = OnlineUsers.GetSessionUser().Id;

            if (string.IsNullOrWhiteSpace(message) || DB.Users.Get(friendId) == null)
            {
                return Json(new { success = false, error = "Message invalide ou ami inexistant" });
            }

            try
            {
                UserChats newMessage = new UserChats
                {
                    UserId = userId,
                    FriendId = friendId,
                    Message = message,
                    Date = DateTime.Now,
                    isModified = false,
                    IsRead = false,
                };
                //DB.UserChats.Add(newMessage);
                DB.UserChats.Add(newMessage);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Une erreur s'est produite lors de l'envoi du message" });
            }
        }

        public ActionResult UpdateMessage(int messageId, string message)
        {
            int userId = OnlineUsers.GetSessionUser().Id;
            try
            {
                var m = DB.UserChats.Get(messageId);
                if (m != null && m.UserId == userId)
                {

                    m.Message = message;
                    m.isModified = true;


                    DB.UserChats.Update(m);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Une erreur s'est produite lors de la modification du nessage" });

            }

        }
        public ActionResult DeleteMessage(int messageId)
        {
            int userId = OnlineUsers.GetSessionUser().Id;

            try
            {
                var m = DB.UserChats.Get(messageId);
                if (m != null && m.UserId == userId)
                {
                    DB.UserChats.Delete(messageId);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Une erreur s'est produite lors de la suppression du nessage" });

            }

        }

        [OnlineUsers.AdminAccess]
        public ActionResult DeleteChat(int chatid)
        {
            var user = OnlineUsers.GetSessionUser();
            try
            {
                var m = DB.UserChats.Get(chatid);
                if (m != null && user.IsAdmin)
                {
                    DB.UserChats.Delete(chatid);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Une erreur s'est produite lors de la suppression du nessage" });

            }
        }

        public JsonResult GetMessage(int messageId)
        {
            int userId = OnlineUsers.GetSessionUser().Id;


            var m = DB.UserChats.Get(messageId);
            if (m != null && m.UserId == userId)
            {
                var message = m.Message;
                return Json(message, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false });
            }


        }
        [OnlineUsers.AdminAccess]
        public ActionResult Logs()
        {
            return View();
        }
        [OnlineUsers.AdminAccess]
        public ActionResult GetChatLogs(bool forceRefresh = false)
        {
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged || DB.UserFriendships.HasChanged || DB.UserChats.HasChanged)
            {
                var chats = DB.UserChats.ToList().OrderByDescending(c => c.Date).ThenBy(c => c.Date.TimeOfDay);
                return PartialView(chats);
            }
            return null;
        }

        public JsonResult IsTyping()
        {
            int friendId = (int)Session["currentChatTarget"];
            int userId = OnlineUsers.GetSessionUser().Id;
            var friendship = DB.Users.GetFriendShip(userId, friendId);
            if (friendship != null && friendship.IsFriend && !friendship.isTyping)
            {
                friendship.isTyping = true;
                DB.UserFriendships.Update(friendship);
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public JsonResult StopTyping()
        {
            int friendId = (int)Session["currentChatTarget"];
            int userId = OnlineUsers.GetSessionUser().Id;
            var friendship = DB.Users.GetFriendShip(userId, friendId);
            if (friendship != null && friendship.IsFriend && friendship.isTyping)
            {
                friendship.isTyping = false;
                DB.UserFriendships.Update(friendship);
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public JsonResult IsTargetTyping()
        {
            int friendId = (int)Session["currentChatTarget"];
            int userId = OnlineUsers.GetSessionUser().Id;
            var friendship = DB.Users.GetFriendShip(friendId, userId);
            if (friendship != null && friendship.IsFriend && friendship.isTyping)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }
    }
}