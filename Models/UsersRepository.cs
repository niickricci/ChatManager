using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace ChatManager.Models
{
    public class UsersRepository : Repository<User>
    {
        public User Create(User user)
        {
            try
            {
                user.Id = base.Add(user);
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add user failed : Message - {ex.Message}");
            }
            return null;
        }
        public override bool Update(User user)
        {
            try
            {
                return base.Update(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update user failed : Message - {ex.Message}");
            }
            return false;
        }
        public override bool Delete(int userId)
        {
            try
            {
                User userToDelete = DB.Users.Get(userId);
                if (userToDelete != null)
                {
                    BeginTransaction();
                    RemoveUnverifiedEmails(userId);
                    RemoveResetPasswordCommands(userId);
                    base.Delete(userId);
                    EndTransaction();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remove user failed : Message - {ex.Message}");
                EndTransaction();
                return false;
            }
        }
        public User FindUser(int id)
        {
            try
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    user.ConfirmEmail = user.Email;
                    user.ConfirmPassword = user.Password;
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Find user failed : Message - {ex.Message}");
                return null;
            }
        }
        public IEnumerable<User> SortedUsers()
        {
            return ToList().OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
        }
        public IEnumerable<User> SortedUsersByFriend()
        {
            return ToList().OrderBy(u => u.FirstName).ThenBy(u => u.LastName).Where(u=>u.IsAdmin);
        }
        public bool Verify_User(int userId, string code)
        {
            User user = Get(userId);
            if (user != null)
            {
                // take the last email verification request
                UnverifiedEmail unverifiedEmail = DB.UnverifiedEmails.ToList().Where(u => u.UserId == userId).FirstOrDefault();
                if (unverifiedEmail != null)
                {
                    if (unverifiedEmail.VerificationCode == code)
                    {
                        try
                        {
                            BeginTransaction();
                            user.Email = user.ConfirmEmail = unverifiedEmail.Email;
                            user.Verified = true;
                            base.Update(user);
                            DB.UnverifiedEmails.Delete(unverifiedEmail.Id);
                            EndTransaction();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Verify_User failed : Message - {ex.Message}");
                            EndTransaction();
                        }
                    }
                }
            }
            return false;
        }
        public bool ChangeEmail(string code)
        {
            UnverifiedEmail unverifiedEmail = FindUnverifiedEmail(code);

            if (unverifiedEmail != null)
            {
                User user = Get(unverifiedEmail.UserId);

                if (user != null)
                {
                    try
                    {
                        BeginTransaction();
                        user.Email = user.ConfirmEmail = unverifiedEmail.Email;
                        user.Verified = true;
                        base.Update(user);
                        DB.UnverifiedEmails.Delete(unverifiedEmail.Id);
                        EndTransaction();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Verify_User failed : Message - {ex.Message}");
                        EndTransaction();
                    }
                }
            }
            return false;
        }
        public bool EmailAvailable(string email, int excludedId = 0)
        {
            User user = ToList().Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user == null)
                return true;
            else
                if (user.Id != excludedId)
                return user.Email.ToLower() != email.ToLower();
            return true;
        }
        public bool EmailExist(string email)
        {
            return ToList().Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault() != null;
        }
        public bool EmailBlocked(string email)
        {
            User user = ToList().Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Blocked;
            return true;
        }
        public bool EmailVerified(string email)
        {
            User user = ToList().Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Verified;
            return false;
        }
        public UnverifiedEmail Add_UnverifiedEmail(int userId, string email)
        {
            try
            {
                BeginTransaction();
                RemoveUnverifiedEmails(userId);
                UnverifiedEmail unverifiedEmail = new UnverifiedEmail() { UserId = userId, Email = email, VerificationCode = Guid.NewGuid().ToString() };
                unverifiedEmail.Id = DB.UnverifiedEmails.Add(unverifiedEmail);
                EndTransaction();
                return unverifiedEmail;
            }
            catch (Exception ex)
            {
                EndTransaction();
                System.Diagnostics.Debug.WriteLine($"Add_UnverifiedEmail failed : Message - {ex.Message}");
                return null;
            }
        }
        //public bool AddJoueur(int currentUserId, int userId)
        //{
        //    User currentUser = DB.Users.Get(currentUserId);
        //    User user = DB.Users.Get(userId);

        //    if (currentUser == null || user == null) { return null; }
        //    if (!currentUser.Friends.Contains(userId) && !user.PendingFriendRequests.Contains(currentUser.Id) && !currentUser.PendingFriendRequests.Contains(userId) && !user.DeclinedBy.Contains(currentUser.Id) && !currentUser.DeclinedBy.Contains(userId))
        //    {
        //        currentUser.PendingFriendRequests.Add(userId);
        //    }
        //    return Json(true, JsonRequestBehavior.AllowGet);
        //}
        public UnverifiedEmail FindUnverifiedEmail(string code)
        {
            return DB.UnverifiedEmails.ToList().Where(u => (u.VerificationCode == code)).FirstOrDefault();
        }
        private void RemoveUnverifiedEmails(int userId)
        {
            List<UnverifiedEmail> UnverifiedEmails = DB.UnverifiedEmails.ToList().Where(u => u.UserId == userId).ToList();
            foreach (UnverifiedEmail UnverifiedEmail in UnverifiedEmails)
            {
                DB.UnverifiedEmails.Delete(UnverifiedEmail.Id);
            }
        }
        public ResetPasswordCommand Add_ResetPasswordCommand(string email)
        {
            try
            {
                User user = DB.Users.ToList().Where(u => u.Email == email).FirstOrDefault();
                if (user != null)
                {
                    BeginTransaction();
                    RemoveResetPasswordCommands(user.Id); // Flush previous request
                    ResetPasswordCommand resetPasswordCommand =
                        new ResetPasswordCommand() { UserId = user.Id, VerificationCode = Guid.NewGuid().ToString() };

                    resetPasswordCommand.Id = DB.ResetPasswordCommands.Add(resetPasswordCommand);
                    EndTransaction();
                    return resetPasswordCommand;
                }
                return null;
            }
            catch (Exception ex)
            {
                EndTransaction();
                System.Diagnostics.Debug.WriteLine($"Add_ResetPasswordCommand failed : Message - {ex.Message}");
                return null;
            }
        }
        public ResetPasswordCommand Find_ResetPasswordCommand(string verificationCode)
        {
            return DB.ResetPasswordCommands.ToList().Where(r => (r.VerificationCode == verificationCode)).FirstOrDefault();
        }
        private void RemoveResetPasswordCommands(int userId)
        {
            List<ResetPasswordCommand> ResetPasswordCommands = DB.ResetPasswordCommands.ToList().Where(r => r.UserId == userId).ToList();
            foreach (ResetPasswordCommand ResetPasswordCommand in ResetPasswordCommands)
            {
                DB.ResetPasswordCommands.Delete(ResetPasswordCommand.Id);
            }
        }
        public bool ResetPassword(int userId, string password)
        {
            User user = Get(userId);
            if (user != null)
            {
                user.Password = user.ConfirmPassword = password;
                try
                {
                    BeginTransaction();
                    RemoveResetPasswordCommands(user.Id);
                    var result = base.Update(user);
                    EndTransaction();
                    return result;
                }
                catch (Exception ex)
                {
                    EndTransaction();
                    System.Diagnostics.Debug.WriteLine($"ResetPassword failed : Message - {ex.Message}");
                }
            }
            return false;
        }

        public User GetUser(LoginCredential loginCredential)
        {
            User user = ToList().Where(u => (u.Email.ToLower() == loginCredential.Email.ToLower()) &&
                                            (u.Password == loginCredential.Password))
                                .FirstOrDefault();
            return user;
        }
        //--------------------------------------Friendships Options-------------------------------------------
        public bool IsFriends(int friendsId)
        {
            var currentUser = OnlineUsers.GetSessionUser();
            var liste = DB.UserFriendships.ToList();
            foreach(var l in liste)
            {
                if(l.UserID == currentUser.Id && l.FriendId==friendsId && l.IsFriend)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsFriendRequestPending(int friendsId)
        {
            var currentUser = OnlineUsers.GetSessionUser();
            var liste = DB.UserFriendships.ToList();
            foreach (var l in liste)
            {
                if (l.UserID == currentUser.Id && l.FriendId == friendsId && l.isPending)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsFriendRequestDeclined(int friendsId)
        {
            var currentUser = OnlineUsers.GetSessionUser();
            var liste = DB.UserFriendships.ToList();
            foreach (var l in liste)
            {
                if (l.UserID == currentUser.Id && l.FriendId == friendsId && l.isDeclined)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasPendingFriendRequest(int friendsId)
        {
            var currentUser = OnlineUsers.GetSessionUser();
            var liste = DB.UserFriendships.ToList();
            foreach (var l in liste)
            {
                if (l.UserID == friendsId && l.FriendId == currentUser.Id && l.isPending)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasDeclinedFriendRequest(int friendsId)
        {
            var currentUser = OnlineUsers.GetSessionUser();
            var liste = DB.UserFriendships.ToList();
            foreach (var l in liste)
            {
                if (l.UserID == currentUser.Id && l.FriendId == friendsId && l.isDeclined)
                {
                    return true;
                }
            }
            return false;
        }
         public void CreateFriendship(int applicantId, int recipientId)
        {
            User userApplicant = DB.Users.Get(applicantId);
            User userRecipient = DB.Users.Get(recipientId);
            var listFrienShips = DB.UserFriendships.ToList();

            if (userApplicant != null && userRecipient != null)
            {
                if (!listFrienShips.Any(l => l.UserID == userApplicant.Id && l.FriendId == userRecipient.Id))
                {
                    var friendshipApplicant = new UserFriendships();
                    friendshipApplicant.UserID = userApplicant.Id;
                    friendshipApplicant.FriendId = userRecipient.Id;
                    friendshipApplicant.IsFriend = false;
                    friendshipApplicant.isPending = false;
                    friendshipApplicant.isDeclined = false;

                    var friendshipRecipient = new UserFriendships();
                    friendshipRecipient.UserID = userRecipient.Id;
                    friendshipRecipient.FriendId = userApplicant.Id;
                    friendshipRecipient.IsFriend = false;
                    friendshipRecipient.isPending = false;
                    friendshipRecipient.isDeclined = false;

                    DB.UserFriendships.Add(friendshipApplicant);
                    DB.UserFriendships.Add(friendshipRecipient);
                }
            }
        }

        public UserFriendships GetFriendShip(int applicantId, int recipientId)
        {
            User userApplicant = DB.Users.Get(applicantId);
            User userRecipient = DB.Users.Get(recipientId);
            var listFrienShips = DB.UserFriendships.ToList();

            if (userApplicant != null && userRecipient != null)
            {
                if (listFrienShips.Any(l => l.UserID == userApplicant.Id && l.FriendId == userRecipient.Id))
                {
                    var friendship = listFrienShips.FirstOrDefault(fs => fs.UserID == userApplicant.Id && fs.FriendId == userRecipient.Id);
                    return friendship;
                }
                return null;
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------
        //---------------------------------------User Chat Options--------------------------------------------
        public IEnumerable<UserChats> GetUsersChats(int applicantId, int recipientId)
        {
            User userApplicant = DB.Users.Get(applicantId);
            User userRecipient = DB.Users.Get(recipientId);
            var listChat = DB.UserChats.ToList();

            if (userApplicant != null && userRecipient != null)
            {
                if (listChat.Any(l => l.UserId == userApplicant.Id && l.FriendId == userRecipient.Id))
                {
                    var applicantMessages = listChat.ToList().Where(c => c.UserId == userApplicant.Id && c.FriendId == userRecipient.Id);
                    return applicantMessages;
                }
                return null;
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------
    }
}