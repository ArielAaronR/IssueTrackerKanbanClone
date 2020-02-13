using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IssueTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Controllers
{
    public class HomeController : Controller
    {
        // GETS the user that is logged in 
        private int? _uid
        {
            get
            {
                return HttpContext.Session.GetInt32("UserId");
            }
        }

        // CHECKS if the user is logged in
        private bool _isLoggedIn
        {
            get
            {
                return HttpContext.Session.GetInt32("UserId") != null;
            }
        }

        private IssueTrackerContext dbContext;
        public HomeController(IssueTrackerContext context)
        {
            // here we can "inject" our context service into the constructor
            dbContext = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("registerdeveloper")]
        public IActionResult RegisterDev()
        {
            return View();
        }

        // Processes User that is Registering
        public IActionResult Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                bool isEmailTaken =
                    dbContext.Users.Any(user => newUser.Email == user.Email);


                if (isEmailTaken)
                {
                    ModelState.AddModelError("Email", "Email Taken");
                }
            }

            if (ModelState.IsValid == false)
            {
                return View("Index");
            }

            // No Errors
            // Hash pw
            PasswordHasher<User> hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);
            newUser.UserPrivilage = 2;
            dbContext.Users.Add(newUser);
            dbContext.SaveChanges();

            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            // AARON MAKE TO CHANGE THE REDIRECTION TO THE PROPER
            return RedirectToAction("Dashboard");
        }

        [HttpPost("registeradmin")]
        public IActionResult RegisterAdmin(User newUser, Admin newAdmin)
        {
            if (ModelState.IsValid)
            {
                bool isEmailTaken = dbContext.Users.Any(user => newUser.Email == user.Email);
                if (isEmailTaken)
                {
                    ModelState.AddModelError("Email", "Email Taken");
                }

            }
            if (ModelState.IsValid == false)
            {
                return View("Index");
            }
            else
            {

                // No Errors
                // Hash pw
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                newUser.Password = hasher.HashPassword(newUser, newUser.Password);
                newUser.UserPrivilage = 1;
                dbContext.Users.Add(newUser);
                dbContext.Admins.Add(newAdmin);
                dbContext.SaveChanges();

                HttpContext.Session.SetInt32("UserId", newUser.UserId);
                return RedirectToAction("Dashboard");
            }

        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }
        // Process a User that is logging in
        [HttpPost("loginUser")]
        public IActionResult LoginUser(LoginUser loginUser)
        {
            string genericError = "Invalid Credentials";

            if (ModelState.IsValid == false)
            {
                return View("Login");
            }

            User dbUser = dbContext.Users.FirstOrDefault(user => loginUser.LoginEmail == user.Email);

            if (dbUser == null)
            {
                ModelState.AddModelError("LoginEmail", genericError);
                return View("Login");
            }

            // dbUser is not null
            // Convert LoginUser to User for PasswordVerification
            User viewUser = new User
            {
                Email = loginUser.LoginEmail,
                Password = loginUser.LoginPassword
            };

            PasswordHasher<User> hasher = new PasswordHasher<User>();

            PasswordVerificationResult passwordCheck =
                hasher.VerifyHashedPassword(viewUser, dbUser.Password, viewUser.Password);

            // failed pw match
            if (passwordCheck == 0)
            {
                ModelState.AddModelError("LoginEmail", genericError);
                return View("Index");
            }

            HttpContext.Session.SetInt32("UserId", dbUser.UserId);
            // AARON MAKE TO CHANGE THE REDIRECTION TO THE PROPER
            // PLACE NOT Home
            return RedirectToAction("Dashboard");
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }

            List<Ticket> allTickets = dbContext.Tickets
            .OrderBy(t => t.Deadline)
            .Include(u => u.AssignedUser)
            .Include(t => t.Creator)
            .ToList();
            User user = dbContext.Users.FirstOrDefault(u => u.UserId == _uid);

            ViewBag.User = user;

            return View(new TicketViewModel { Tickets = allTickets });
        }
        // Logs out a use and clears session

        [HttpGet("dashboard/ticket/{ticketId}")]
        public IActionResult OneTicketDetails(int ticketId)
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            Ticket thisTicket = dbContext.Tickets
            .Include(ticket => ticket.AssignedUser)
            .Include(ticket => ticket.Creator)
            .Include(ticket => ticket.Comments)
            .FirstOrDefault(ticket => ticket.TicketId == ticketId);
            List<User> allUsers = dbContext.Users.ToList();
            User loggedUser = dbContext.Users.FirstOrDefault(u => u.UserId == _uid);

            ViewBag.User = loggedUser;
            return View(new TicketViewModel { Ticket = thisTicket, Users = allUsers });
        }

        [HttpGet("tickets/search")]
        public IActionResult TicketSearch()
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            List<Ticket> allTickets = dbContext.Tickets
            .OrderBy(t => t.CreatedAt)
            .ToList();

            User user = dbContext.Users.FirstOrDefault(u => u.UserId == _uid);

            ViewBag.User = user;
            return View(allTickets);
        }

        [HttpPost("dashboard/tickets/{id}/comments/new")]
        public IActionResult AddComment(int id, TicketViewModel TicketComment)
        {
            Ticket queryTicket = dbContext.Tickets.OrderByDescending(t => t.CreatedAt).Include(t => t.AssignedUser).Include(t => t.Creator).Include(t => t.Comments).FirstOrDefault(t => t.TicketId == id);
            var newId = queryTicket.TicketId;
            List<User> allUsers = dbContext.Users.ToList();
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            if (TicketComment.Comment.Description == null)
            {
                ModelState.AddModelError("Comment.Content", "Message content field cannot be empty.");
                return View("OneTicketDetails", new TicketViewModel { Ticket = queryTicket, Users = allUsers });
            }

            if (ModelState.IsValid)
            {

                Ticket thisTicket = dbContext.Tickets.FirstOrDefault(t => t.TicketId == id);
                User thisUser = dbContext.Users.FirstOrDefault(user => user.UserId == _uid);
                Comment newComment = new Comment();
                newComment.Description = TicketComment.Comment.Description;
                newComment.UserId = thisUser.UserId;
                newComment.TicketId = thisTicket.TicketId;
                newComment.CommentedFromUser = thisUser;
                newComment.CommentedTicket = thisTicket;
                dbContext.Add(newComment);
                dbContext.SaveChanges();


                return RedirectToAction("Dashboard");

            }
            else
            {
                return View("OneTicketDetails", new TicketViewModel { Ticket = queryTicket, Users = allUsers });
            }
        }


        [HttpPost("tickets/{ticketId}/comments/{id}/delete")]
        public IActionResult DeleteComment(int ticketId, int id)
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Dashboard");
            }

            Comment thisComment = dbContext.Comments.FirstOrDefault(comment => comment.CommentId == id);

            dbContext.Comments.Remove(thisComment);

            Ticket thisTicket = dbContext.Tickets.FirstOrDefault(ticket => ticket.TicketId == ticketId);
            var newId = thisTicket.TicketId;

            return RedirectToAction("OneTicketDetails", new { Id = newId });


        }

        [HttpGet("tickets/new")]
        public IActionResult NewTicket()
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            List<User> allUsers = dbContext.Users.ToList();
            User user = dbContext.Users.FirstOrDefault(u => u.UserId == _uid);

            ViewBag.User = user;
            return View(new TicketViewModel { Users = allUsers });
        }
        [HttpPost("tickets/new")]
        public IActionResult CreateTicket(TicketViewModel newTicket)
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                User ticketCreator = dbContext.Users.FirstOrDefault(u => u.UserId == _uid);
                Admin Creator = dbContext.Admins.FirstOrDefault(a => a.AdminId == _uid);
                User AssignedUser = dbContext.Users.FirstOrDefault(u => u.UserId == newTicket.Ticket.UserId);
                if (ticketCreator.UserPrivilage == 1)
                {
                    if (newTicket.Ticket.Deadline > DateTime.Now)
                    {
                        newTicket.Ticket.AssignedUser = AssignedUser;
                        newTicket.Ticket.Creator = Creator;
                        newTicket.Ticket.Creator.UserId = ticketCreator.UserId;
                        dbContext.Add(newTicket.Ticket);
                        dbContext.SaveChanges();
                        return RedirectToAction("Dashboard");

                    }
                    else
                    {
                        ModelState.AddModelError("Ticket.Deadline", "Deadling must be set in to the future");
                        List<User> allUsers = dbContext.Users.ToList();
                        return View("NewTicket", new TicketViewModel { Users = allUsers });

                    }
                }
                else
                {
                    ModelState.AddModelError("Ticket.UserId", "Need amin privileges to create/assign a new ticket");
                    List<User> allUsers = dbContext.Users.ToList();
                    return View("NewTicket", new TicketViewModel { Users = allUsers });

                }
            }
            else
            {
                List<User> allUsers = dbContext.Users.ToList();
                return View("NewTicket", new TicketViewModel { Users = allUsers });
            }
        }

        [HttpGet("ticket/{ticketId}/edit")]
        public IActionResult EditTicket(int ticketId)
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            Ticket thisTicket = dbContext.Tickets
            .Include(t => t.AssignedUser)
            .FirstOrDefault(t => t.TicketId == ticketId);

            List<User> allUsers = dbContext.Users.ToList();

            User user = dbContext.Users.FirstOrDefault(u => u.UserId == _uid);

            ViewBag.User = user;
            return View(new TicketViewModel { Ticket = thisTicket, Users = allUsers });
        }

        [HttpPost("tickets/{id}/edit")]
        public IActionResult EditTicketProcess(int id, TicketViewModel updateTicket)
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Dashboard");
            }
            else
            {
                Ticket thisTicket = dbContext.Tickets.FirstOrDefault(ticket => ticket.TicketId == id);
                thisTicket.Status = updateTicket.Ticket.Status;
                dbContext.Update(thisTicket);
                dbContext.SaveChanges();
                return RedirectToAction("Dashboard");
            }
        }
        [HttpPost("tickets/{id}/edit/admin")]
        public IActionResult EditTicketAdmin(int id, TicketViewModel updateTicket)
        {
            if (!_isLoggedIn)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                if (updateTicket.Ticket.Deadline > DateTime.Now)
                {
                    Ticket thisTicket = dbContext.Tickets.FirstOrDefault(ticket => ticket.TicketId == id);
                    User assignedUser = dbContext.Users.FirstOrDefault(user => user.UserId == updateTicket.UserId);
                    User ticketCreator = dbContext.Users.FirstOrDefault(user => user.UserId == _uid);
                    Admin Creator = dbContext.Admins.FirstOrDefault(admin => admin.AdminId == _uid);
                    thisTicket.Title = updateTicket.Ticket.Title;
                    thisTicket.Description = updateTicket.Ticket.Description;
                    thisTicket.Priority = updateTicket.Ticket.Priority;
                    thisTicket.Deadline = updateTicket.Ticket.Deadline;
                    thisTicket.Status = updateTicket.Ticket.Status;
                    thisTicket.UserId = updateTicket.Ticket.UserId;
                    thisTicket.Creator = Creator;
                    thisTicket.Creator.UserId = ticketCreator.UserId;
                    thisTicket.AssignedUser = assignedUser;
                    thisTicket.UpdatedAt = DateTime.Now;
                    dbContext.Update(thisTicket);
                    dbContext.SaveChanges();
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    ModelState.AddModelError("Ticket.Deadline", "Deadline must be set to a future date.");
                    Ticket thisTicket = dbContext.Tickets.Include(ticket => ticket.AssignedUser).FirstOrDefault(ticket => ticket.TicketId == id);
                    List<User> allUsers = dbContext.Users.ToList();
                    return RedirectToAction("EditTicket", new TicketViewModel { Ticket = thisTicket, Users = allUsers });
                }
            }
            Ticket thiseditTicket = dbContext.Tickets.Include(ticket => ticket.AssignedUser).FirstOrDefault(ticket => ticket.TicketId == id);
            List<User> alleditUsers = dbContext.Users.ToList();
            return RedirectToAction("EditTicket", new TicketViewModel { Ticket = thiseditTicket, Users = alleditUsers });
        }



        [HttpPost("tickets/{id}/delete")]
        public IActionResult DeleteTicket(int id)
        {
            if (HttpContext.Session.GetString("UserName") == "Guest")
            {
                return RedirectToAction("Dashboard");
            }
            else
            {
                Ticket thisTicket = dbContext.Tickets.FirstOrDefault(ticket => ticket.TicketId == id);
                dbContext.Tickets.Remove(thisTicket);
                dbContext.SaveChanges();
                return RedirectToAction("Dashboard");
            }
        }
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }









        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
