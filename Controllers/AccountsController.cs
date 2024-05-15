﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.WebPages.Html;
using ChatManager.Models;
using Mail;


namespace ChatManager.Controllers
{
    public class AccountsController : Controller
    {

        #region Account creation
        [HttpPost]
        public JsonResult EmailAvailable(string email)
        {
           
            User user = OnlineUsers.GetSessionUser();
            int excludedId = user != null ? user.Id : 0;
            if(Session["editTarget"] != null){
                excludedId = Convert.ToInt32(Session["editTarget"]);
            }
            return Json(DB.Users.EmailAvailable(email, excludedId));
        }

        [HttpPost]
        public JsonResult EmailExist(string email)
        {
            return Json(DB.Users.EmailExist(email));
        }

        public ActionResult Subscribe()
        {
            ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
            User user = new User();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Subscribe(User user)
        {
            user.UserTypeId = 3; // self subscribed user 
            if (ModelState.IsValid)
            {
                if (user.Avatar == Models.User.DefaultImage)
                {
                    // required avatar image
                    ModelState.AddModelError("Avatar", "Veuillez choisir votre avatar");
                    ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
                }
                else
                {
                    user = DB.Users.Create(user);
                    if (user != null)
                    {
                        SendEmailVerification(user, user.Email);
                        return RedirectToAction("SubscribeDone/" + user.Id.ToString());
                    }
                    else
                        return RedirectToAction("Report", "Errors", new { message = "Échec de création de compte" });
                }
            }
            return View(user);
        }

        public ActionResult SubscribeDone(int id)
        {
            User newlySubscribedUser = DB.Users.Get(id);
            if (newlySubscribedUser != null)
                return View(newlySubscribedUser);
            return RedirectToAction("Login");
        }
        #endregion

        #region Account Verification
        public void SendEmailVerification(User user, string newEmail)
        {
            if (user.Id != 0)
            {
                UnverifiedEmail unverifiedEmail = DB.Users.Add_UnverifiedEmail(user.Id, newEmail);
                if (unverifiedEmail != null)
                {
                    string verificationUrl = Url.Action("VerifyUser", "Accounts", null, Request.Url.Scheme);
                    String Link = @"<br/><a href='" + verificationUrl + "?code=" + unverifiedEmail.VerificationCode + @"' > Confirmez votre inscription...</a>";

                    String suffixe = "";
                    if (user.GenderId == 2)
                    {
                        suffixe = "e";
                    }
                    string Subject = "Movies Database - Vérification d'inscription...";

                    string Body = "Bonjour " + user.GetFullName(true) + @",<br/><br/>";
                    Body += @"Merci de vous être inscrit" + suffixe + " au site ChatManager. <br/>";
                    Body += @"Pour utiliser votre compte vous devez confirmer votre inscription en cliquant sur le lien suivant : <br/>";
                    Body += Link;
                    Body += @"<br/><br/>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.";
                    Body += @"<br/><br/>Si vous éprouvez des difficultés ou s'il s'agit d'une erreur, veuillez le signaler à <a href='mailto:"
                         + SMTP.OwnerEmail + "'>" + SMTP.OwnerName + "</a> (Webmestre du site ChatManager)";

                    SMTP.SendEmail(user.GetFullName(), unverifiedEmail.Email, Subject, Body);
                }
            }
        }
        public ActionResult VerifyDone(int id)
        {
            User newlySubscribedUser = DB.Users.Get(id);
            if (newlySubscribedUser != null)
                return View(newlySubscribedUser);
            return RedirectToAction("Login");
        }
        public ActionResult VerifyError()
        {
            return View();
        }
        public ActionResult AlreadyVerified()
        {
            return View();
        }
        public ActionResult VerifyUser(string code)
        {
            UnverifiedEmail UnverifiedEmail = DB.Users.FindUnverifiedEmail(code);
            if (UnverifiedEmail != null)
            {
                User newlySubscribedUser = DB.Users.Get(UnverifiedEmail.UserId);

                if (newlySubscribedUser != null)
                {
                    if (DB.Users.EmailVerified(newlySubscribedUser.Email))
                        return RedirectToAction("AlreadyVerified");

                    if (DB.Users.Verify_User(newlySubscribedUser.Id, code))
                        return RedirectToAction("VerifyDone/" + newlySubscribedUser.Id);
                }
                else
                    RedirectToAction("VerifyError");
            }
            return RedirectToAction("VerifyError");
        }
        #endregion

        #region EmailChange
        public ActionResult EmailChangedAlert()
        {
            OnlineUsers.RemoveSessionUser();
            return View();
        }
        public ActionResult CommitEmailChange(string code)
        {
            if (DB.Users.ChangeEmail(code))
            {
                return RedirectToAction("EmailChanged");
            }
            return RedirectToAction("EmailChangedError");
        }
        public ActionResult EmailChanged()
        {
            return View();
        }
        public ActionResult EmailChangedError()
        {
            return View();
        }
        public void SendEmailChangedVerification(User user, string newEmail)
        {
            if (user.Id != 0)
            {
                UnverifiedEmail unverifiedEmail = DB.Users.Add_UnverifiedEmail(user.Id, newEmail);
                if (unverifiedEmail != null)
                {
                    string verificationUrl = Url.Action("CommitEmailChange", "Accounts", null, Request.Url.Scheme);
                    String Link = @"<br/><a href='" + verificationUrl + "?code=" + unverifiedEmail.VerificationCode + @"' > Confirmez votre adresse...</a>";

                    string Subject = "ChatManager - Confirmation de changement de courriel...";

                    string Body = "Bonjour " + user.GetFullName(true) + @",<br/><br/>";
                    Body += @"Vous avez modifié votre adresse de courriel. <br/>";
                    Body += @"Pour que ce changement soit pris en compte, vous devez confirmer cette adresse en cliquant sur le lien suivant : <br/>";
                    Body += Link;
                    Body += @"<br/><br/>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.";
                    Body += @"<br/><br/>Si vous éprouvez des difficultés ou s'il s'agit d'une erreur, veuillez le signaler à <a href='mailto:"
                         + SMTP.OwnerEmail + "'>" + SMTP.OwnerName + "</a> (Webmestre du site ChatManager)";

                    SMTP.SendEmail(user.GetFullName(), unverifiedEmail.Email, Subject, Body);
                }
            }
        }
        #endregion

        #region ResetPassword
        public ActionResult ResetPasswordCommand()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ResetPasswordCommand(string Email)
        {
            if (ModelState.IsValid)
            {
                SendResetPasswordCommandEmail(Email);
                return RedirectToAction("ResetPasswordCommandAlert");
            }
            return View(Email);
        }
        public void SendResetPasswordCommandEmail(string email)
        {
            ResetPasswordCommand resetPasswordCommand = DB.Users.Add_ResetPasswordCommand(email);
            if (resetPasswordCommand != null)
            {
                User user = DB.Users.Get(resetPasswordCommand.UserId);
                string verificationUrl = Url.Action("ResetPassword", "Accounts", null, Request.Url.Scheme);
                String Link = @"<br/><a href='" + verificationUrl + "?code=" + resetPasswordCommand.VerificationCode + @"' > Réinitialisation de mot de passe...</a>";

                string Subject = "Répertoire de films - Réinitialisaton ...";

                string Body = "Bonjour " + user.GetFullName(true) + @",<br/><br/>";
                Body += @"Vous avez demandé de réinitialiser votre mot de passe. <br/>";
                Body += @"Procédez en cliquant sur le lien suivant : <br/>";
                Body += Link;
                Body += @"<br/><br/>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.";
                Body += @"<br/><br/>Si vous éprouvez des difficultés ou s'il s'agit d'une erreur, veuillez le signaler à <a href='mailto:"
                     + SMTP.OwnerEmail + "'>" + SMTP.OwnerName + "</a> (Webmestre du site [nom de l'application])";

                SMTP.SendEmail(user.GetFullName(), user.Email, Subject, Body);
            }
        }
        public ActionResult ResetPassword(string code)
        {
            ResetPasswordCommand resetPasswordCommand = DB.Users.Find_ResetPasswordCommand(code);
            if (resetPasswordCommand != null)
                return View(new PasswordView() { Code = code });
            return RedirectToAction("ResetPasswordError");
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult ResetPassword(PasswordView passwordView)
        {
            if (ModelState.IsValid)
            {
                ResetPasswordCommand resetPasswordCommand = DB.Users.Find_ResetPasswordCommand(passwordView.Code);
                if (resetPasswordCommand != null)
                {
                    if (DB.Users.ResetPassword(resetPasswordCommand.UserId, passwordView.Password))
                        return RedirectToAction("ResetPasswordSuccess");
                    else
                        return RedirectToAction("ResetPasswordError");
                }
                else
                    return RedirectToAction("ResetPasswordError");
            }
            return View(passwordView);
        }
        public ActionResult ResetPasswordCommandAlert()
        {
            return View();
        }
        public ActionResult ResetPasswordSuccess()
        {
            return View();
        }
        public ActionResult ResetPasswordError()
        {
            return View();
        }
        #endregion

        #region Profil
        [OnlineUsers.UserAccess]
        public ActionResult Profil()
        {
            User userToEdit = OnlineUsers.GetSessionUser().Clone();
            if (userToEdit != null)
            {
                ViewBag.Genders = new SelectList(DB.Genders.ToList(), "Id", "Name", userToEdit.GenderId);
                Session["UnchangedPasswordCode"] = Guid.NewGuid().ToString().Substring(0, 12);
                userToEdit.Password = userToEdit.ConfirmPassword = (string)Session["UnchangedPasswordCode"];
                return View(userToEdit);
            }
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Profil(User user)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            user.Id = currentUser.Id;
            user.Verified = currentUser.Verified;
            user.UserTypeId = currentUser.UserTypeId;
            user.Blocked = currentUser.Blocked;
            user.CreationDate = currentUser.CreationDate;

            string newEmail = "";
            if (ModelState.IsValid)
            {
                if (user.Password == (string)Session["UnchangedPasswordCode"])
                    user.Password = user.ConfirmPassword = currentUser.Password;

                if (user.Email != currentUser.Email)
                {
                    newEmail = user.Email;
                    user.Email = user.ConfirmEmail = currentUser.Email;
                }

                if (DB.Users.Update(user))
                {
                    if (newEmail != "")
                    {
                        SendEmailChangedVerification(user, newEmail);
                        return RedirectToAction("EmailChangedAlert");
                    }
                    else
                        return Redirect((string)Session["LastAction"]);
                }
            }
            ViewBag.Genders = new SelectList(DB.Genders.ToList(), "Id", "Name", user.GenderId);
            return View(currentUser);
        }
        #endregion

        #region Login and Logout

        public ActionResult ExpiredSession()
        {
            OnlineUsers.RemoveSessionUser();
            return Redirect("/Accounts/Login?message=Session expirée, veuillez vous reconnecter.");
        }
        public ActionResult Login(string message)
        {
            ViewBag.Message = message;
            OnlineUsers.RemoveSessionUser();
            return View(new LoginCredential());
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Login(LoginCredential loginCredential)
        {
            if (ModelState.IsValid)
            {
                if (DB.Users.EmailBlocked(loginCredential.Email))
                {
                    ModelState.AddModelError("Email", "Ce compte est bloqué.");
                    return View(loginCredential);
                }
                if (!DB.Users.EmailVerified(loginCredential.Email))
                {
                    ModelState.AddModelError("Email", "Ce courriel n'est pas vérifié.");
                    return View(loginCredential);
                }
                User user = DB.Users.GetUser(loginCredential);

                if (user == null)
                {
                    ModelState.AddModelError("Password", "Mot de passe incorrect.");
                    return View(loginCredential);
                }
                OnlineUsers.AddSessionUser(user.Id);
                OnlineUsers.AddNotification(user.Id, "Heureux de vous revoir");

                //---------------------UsersLogsJournal-------------------------
                UsersLogsJournal loginData = new UsersLogsJournal
                {
                    UserID = user.Id,
                    LoginTime = DateTime.Now,
                    LogoutTime = DateTime.Now,
                };
                DB.UserLogs.Add(loginData);
                //--------------------------------------------------------------

                return RedirectToAction("Index", "ChatRoom");
            }

            return View(loginCredential);
        }
        public ActionResult Logout()
        {
            //---------------------UsersLogsJournal-------------------------
            User user = OnlineUsers.GetSessionUser();
            UsersLogsJournal loginData = new UsersLogsJournal
            {
                UserID = user.Id,
                LogoutTime = DateTime.Now,
            };
            DB.UserLogs.Update(loginData);
            //--------------------------------------------------------------
            OnlineUsers.RemoveSessionUser();
            return RedirectToAction("Login");
        }

        [OnlineUsers.AdminAccess]
        public ActionResult LoginsJournal()
        {
            return View();
        }
        #endregion
        [OnlineUsers.AdminAccess]
        public ActionResult GroupEmail(string status = "")
        {
            ViewBag.SelectedUsers = new List<int>();
            ViewBag.Users = DB.Users.SortedUsers();
            ViewBag.Status = status;
            return View(new GroupEmail() { Message = "Bonjour [Nom]," });
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult GroupEmail(GroupEmail groupEmail)
        {
            if (ModelState.IsValid)
            {
                groupEmail.Send();
                return RedirectToAction("GroupEmail", new { status = "Message envoyé avec succès." });
            }
            ViewBag.Users = DB.Users.SortedUsers();
            return View(groupEmail);
        }
        #region Administrator actions
        public JsonResult NeedUpdate()
        {
            return Json(OnlineUsers.HasChanged(), JsonRequestBehavior.AllowGet);
        }

        [OnlineUsers.AdminAccess]
        public void SendBlockStatusEMail(User user)
        {
            User currentAdmin = OnlineUsers.GetSessionUser();
            if (user != null)
            {
                if (user.Blocked)
                {
                    string Subject = "Movies Database - Compte bloqué...";

                    string Body = "Désolé " + user.GetFullName(true) + @"<br/><br/>";
                    Body += @"Votre compte a été bloqué par le modérateur suite à des abus de votre part. <br/>";
                    Body += @"Pour pour plus d'informations veuillez écrire à <a href='mailto:" + currentAdmin.Email + @"'>" + currentAdmin.GetFullName(false) + "</a><br/>";

                    SMTP.SendEmail(user.GetFullName(), user.Email, Subject, Body);
                }
                else
                {
                    string Subject = "Movies Database - Compte débloqué...";

                    string Body = "Bonjour " + user.GetFullName(true) + @"<br/><br/>";
                    Body += @"Votre compte a été débloqué par le modérateur <br/><br/>";
                    Body += @"Bonne journée! <br/>";

                    SMTP.SendEmail(user.GetFullName(), user.Email, Subject, Body);
                }
            }
        }
        [OnlineUsers.AdminAccess]
        public JsonResult ChangeUserBlockedStatus(int userid, bool blocked)
        {
            User user = DB.Users.Get(userid);
            user.Blocked = blocked;
            SendBlockStatusEMail(user);
            return Json(DB.Users.Update(user), JsonRequestBehavior.AllowGet);
        }
        public JsonResult PromoteUser(int userid)
        {
            User user = DB.Users.Get(userid);
            if (user != null)
            {
                user.UserTypeId--;
                if (user.UserTypeId <= 0)
                    user.UserTypeId = 3;
                DB.Users.Update(user);
            }
            return Json(DB.Users.Update(user), JsonRequestBehavior.AllowGet);
        }
        [OnlineUsers.AdminAccess]
        public void SendDeletedAccountEMail(User user)
        {
            User currentAdmin = OnlineUsers.GetSessionUser();
            if (user != null)
            {
                string Subject = "Movies Database - Compte bloqué...";

                string Body = "Désolé " + user.GetFullName(true) + @"<br/><br/>";
                Body += @"Votre compte a été retiré par le modérateur suite à des abus de votre part. <br/>";
                Body += @"Pour pour plus d'informations veuillez écrire à <a href='mailto:" + currentAdmin.Email + @"'>" + currentAdmin.GetFullName(false) + "</a><br/>";

                SMTP.SendEmail(user.GetFullName(), user.Email, Subject, Body);
            }
        }
        [OnlineUsers.AdminAccess]
        public JsonResult Delete(int userid)
        {
            User userToDelete = DB.Users.Get(userid);
            if (userToDelete != null)
            {
                SendDeletedAccountEMail(userToDelete);
                return Json(DB.Users.Delete(userid), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
        [OnlineUsers.AdminAccess]
        public ActionResult UsersList()
        {
            return View();
        }
        [OnlineUsers.AdminAccess]
        public ActionResult GetUsersList(bool forceRefresh = false)
        {
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged)
            {
                return PartialView(DB.Users.SortedUsers());
            }
            return null;
        }

        public ActionResult GetLoginsJournal(bool forceRefresh = false)
        {
            if (forceRefresh || OnlineUsers.HasChanged() || DB.Users.HasChanged)
            {
                return PartialView(DB.Users.SortedUsers());
            }
            return null;
        }

        [OnlineUsers.AdminAccess]
        public JsonResult DeleteLogs(DateTime date)
        {
            var alllogs = DB.UserLogs.ToList();
            alllogs = alllogs.Where(l => l.LoginTime.Date == date.Date).ToList();
            foreach (var log in alllogs)
            {
                DB.UserLogs.Delete(log.Id);
            }
            return Json(true, JsonRequestBehavior.AllowGet);

        }
        #endregion
        [OnlineUsers.AdminAccess]
        public ActionResult Edit(int id)
        {
            var user = DB.Users.FindUser(id);

            if (user != null)
            {
                Session["editTarget"] = id;
                ViewBag.UserTypes = new SelectList(DB.UserTypes.ToList(), "Id", "Name", user.UserTypeId);
                Session["LastAction"] = "/Friendships/FriendsList";
                return View(user);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [OnlineUsers.AdminAccess]
        public ActionResult Edit(User user)
        {
            var oldUser = DB.Users.FindUser(user.Id);
            oldUser.Id = user.Id;
            oldUser.FirstName = user.FirstName; 
            oldUser.LastName = user.LastName;
            oldUser.Verified = user.Verified;
            oldUser.UserTypeId = user.UserTypeId;
            oldUser.Blocked = user.Blocked;
            oldUser.Avatar = user.Avatar;
            //oldUser.CreationDate = user.CreationDate;

            string newEmail = "";
                if (oldUser.Email != user.Email)
                {
                    newEmail = user.Email;
                    oldUser.Email = oldUser.ConfirmEmail = user.Email;
                }

                if (DB.Users.Update(oldUser))
                {
                    if (newEmail != "")
                    {
                        SendEmailChangedVerification(oldUser, newEmail);
                        return Redirect((string)Session["LastAction"]);
                    }
                    else
                        return Redirect((string)Session["LastAction"]);
                }
            
            ViewBag.UserTypes = new SelectList(DB.UserTypes.ToList(), "Id", "Name", user.UserTypeId);
            return View(user);
        }

        public JsonResult UserHasNotifications()
        {
            var messages = DB.UserChats.ToList().Where(m=>m.FriendId == OnlineUsers.GetSessionUser().Id);
            if (messages.Any(m => m.IsRead == false))
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            //if (!OnlineUsers.GetSessionUser().HasNotification) { 
            //    return Json(true, JsonRequestBehavior.AllowGet); 
            //} 
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUserNotifications()
        {
            var frenchDates = new System.Globalization.CultureInfo("fr-FR");
            var messages = DB.UserChats.ToList().Where(m => m.FriendId == OnlineUsers.GetSessionUser().Id);
            var msgNotRead = messages.Where(m => m.IsRead == false);
            msgNotRead = msgNotRead.OrderByDescending(m => m.Date).ThenBy(m => m.Date.TimeOfDay);
            List<string> listMsg = new List<string> { };

            foreach (var msg in msgNotRead)
            {
                listMsg.Add($"[ {msg.Date.ToString("d/MM/yy - HH:mm", frenchDates)} ] Nouveau message de {DB.Users.FindUser(msg.UserId).GetFullName()}");
                //listMsg.Add($"Nouveau message de {DB.Users.FindUser(msg.UserId).GetFullName()}{msg.Date.ToString("d MMMM yyyy - HH:mm", frenchDates)} - {DB.Users.FindUser(msg.UserId).GetFullName()}: {msg.Message}");
            }

            return Json(listMsg, JsonRequestBehavior.AllowGet);
        }
    }
}