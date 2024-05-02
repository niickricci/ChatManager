using ChatManager.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace MoviesDBManager.Controllers
{
    [OnlineUsers.UserAccess]
    public class FriendShipsController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult FriendsList()
        {
            return View();
        }

        public ActionResult GetFriendList(bool forceRefresh = false, string[] filterType = null, string keyword = "")
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
                                filteredUsers.AddRange(users.Where(u => DB.UserFriendships.ToList().Any(uf => uf.UserID == u.Id && uf.FriendId == OnlineUsers.GetSessionUser().Id && uf.IsFriend) && !u.Blocked).Where(u=> u.GetFullName().ToLower().Contains(keyword.ToLower())));
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



        //Friendships
        public JsonResult AddUser(int userId)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            User user = DB.Users.Get(userId);


            if (user != null && currentUser != null)
            {
                if (DB.Users.GetFriendShip(currentUser.Id, userId) == null)
                {
                    DB.Users.CreateFriendship(currentUser.Id, userId);

                    var applicantFriendship = DB.Users.GetFriendShip(currentUser.Id, userId);
                    var recipientFriendship = DB.Users.GetFriendShip(userId, currentUser.Id);

                    applicantFriendship.IsFriend = false;
                    applicantFriendship.isPending = true;
                    applicantFriendship.isDeclined = false;

                    recipientFriendship.IsFriend = false;
                    recipientFriendship.isPending = false;
                    recipientFriendship.isDeclined = false;

                    DB.UserFriendships.Update(applicantFriendship);
                    DB.UserFriendships.Update(recipientFriendship);

                    return Json(true, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    var applicantFriendship = DB.Users.GetFriendShip(currentUser.Id, userId);
                    var recipientFriendship = DB.Users.GetFriendShip(userId, currentUser.Id);

                    applicantFriendship.IsFriend = false;
                    applicantFriendship.isPending = true;
                    applicantFriendship.isDeclined = false;

                    recipientFriendship.IsFriend = false;
                    recipientFriendship.isPending = false;
                    recipientFriendship.isDeclined = false;

                    DB.UserFriendships.Update(applicantFriendship);
                    DB.UserFriendships.Update(recipientFriendship);

                    return Json(true, JsonRequestBehavior.AllowGet);
                }

            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RemoveFriend(int userId)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            User user = DB.Users.Get(userId);

            if (user != null && currentUser != null)
            {
                var applicantFriendship = DB.Users.GetFriendShip(userId, currentUser.Id);
                var recipientFriendship = DB.Users.GetFriendShip(currentUser.Id, userId); ;

                if (applicantFriendship != null && recipientFriendship != null)
                {
                    //Mettre a jour les friendships 
                    //applicantFriendship.IsFriend = false;
                    //applicantFriendship.isPending = false;
                    //applicantFriendship.isDeclined = false;

                    //recipientFriendship.IsFriend = false;
                    //recipientFriendship.isPending = false;
                    //recipientFriendship.isDeclined = false;

                    DB.UserFriendships.Delete(applicantFriendship.Id);
                    DB.UserFriendships.Delete(recipientFriendship.Id);

                    return Json(DB.UserFriendships.Delete(applicantFriendship.Id) &&
                    DB.UserFriendships.Delete(recipientFriendship.Id), JsonRequestBehavior.AllowGet);
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AcceptFriendsRequest(int userId)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            User user = DB.Users.Get(userId);

            if (user != null && currentUser != null)
            {
                var applicantFriendship = DB.Users.GetFriendShip(userId, currentUser.Id);
                var recipientFriendship = DB.Users.GetFriendShip(currentUser.Id, userId); ;

                if (applicantFriendship != null && recipientFriendship != null)
                {
                    //Mettre a jour les friendships 
                    applicantFriendship.IsFriend = true;
                    applicantFriendship.isPending = false;
                    applicantFriendship.isDeclined = false;

                    recipientFriendship.IsFriend = true;
                    recipientFriendship.isPending = false;
                    recipientFriendship.isDeclined = false;

                    DB.UserFriendships.Update(applicantFriendship);
                    DB.UserFriendships.Update(recipientFriendship);

                    return Json(true, JsonRequestBehavior.AllowGet);
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CancelFriendsRequest(int userId)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            User user = DB.Users.Get(userId);

            if (user != null && currentUser != null)
            {
                var recipientFriendship = DB.Users.GetFriendShip(userId, currentUser.Id);
                var applicantFriendship = DB.Users.GetFriendShip(currentUser.Id, userId); ;

                if (applicantFriendship != null && recipientFriendship != null)
                {
                    DB.UserFriendships.Delete(applicantFriendship.Id);
                    DB.UserFriendships.Delete(recipientFriendship.Id);

                    return Json(true, JsonRequestBehavior.AllowGet);
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DeclineFriendsRequest(int userId)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            User user = DB.Users.Get(userId);

            if (user != null && currentUser != null)
            {
                var applicantFriendship = DB.Users.GetFriendShip(userId, currentUser.Id);
                var recipientFriendship = DB.Users.GetFriendShip(currentUser.Id, userId); ;

                if (applicantFriendship != null && recipientFriendship != null)
                {
                    //Mettre a jour les friendships 
                    applicantFriendship.IsFriend = false;
                    applicantFriendship.isPending = false;
                    applicantFriendship.isDeclined = true;

                    recipientFriendship.IsFriend = false;
                    recipientFriendship.isPending = false;
                    recipientFriendship.isDeclined = false;

                    DB.UserFriendships.Update(applicantFriendship);
                    DB.UserFriendships.Update(recipientFriendship);

                    return Json(true, JsonRequestBehavior.AllowGet);
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }
    }

}