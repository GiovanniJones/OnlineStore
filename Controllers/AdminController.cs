﻿using EC.Models;
using EC.Models.Context;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Collections.Generic;

namespace EC.Controllers
{
    
    [Authorize(Roles = "Administrator,Employee, Customer")]
    public class AdminController : Controller
    {
        private ApplicationDbContext context = ApplicationDbContext.Create();
        private UserContext UserContext = new UserContext();
        private UserManager<ApplicationUser> manager;
        private static bool initialized = false;
        RoleManager<IdentityRole> roleManager;
        int PageSize = 10; 
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            var roleStore = new RoleStore<IdentityRole>(context: context);
            var roles = roleStore.Roles.Select((m => new RolesDataSet { Id = m.Id, Role = m.Name })).Select(m => m.Role).ToList();  
            ViewBag.roles = roles; 
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Email,Password,PasswordConfirmation,Roles")] AdminModels model)
        {
            Initialize();
            if (model.Roles == null)
            {
                ModelState.AddModelError("", "At least one role has to be selected");
                return View(model);
            }
            if(ModelState.IsValid)
            { 
                var user = new ApplicationUser {UserName = model.Email, Email = model.Email };
                IdentityResult result = await manager.CreateAsync(user, model.Password);
                IdentityResult roleResult = new IdentityResult(); 
                if (result.Succeeded)
                {
                    roleResult = await manager.AddToRolesAsync(userId: user.Id, model.Roles.ToArray()); 
                    var code = await manager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    //await manager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
                    return RedirectToAction("ListUsers");
                }
                AddError(roleResult);
                AddError(result);             
            }
            
            return View(model);
        }

        
        public ActionResult ListUsers(string SortOrder, int? page, string search)
        {
            Initialize();
            ViewBag.Sort = SortOrder;
            int pageNumber = (page ?? 1);
            switch (SortOrder)
            {
                case "Descending":
                    return View(manager.Users.Select(m => new UserView { Email = m.Email, Phone = m.PhoneNumber, UserName = m.UserName }).OrderByDescending(s => s.Email).ToPagedList(pageNumber, PageSize));
                default:
                    return  View(manager.Users.Select(m => new UserView { Email = m.Email, Phone = m.PhoneNumber, UserName = m.UserName }).OrderBy(s => s.Email).ToPagedList(pageNumber, PageSize));
            }
          
        }

        public ActionResult UserDetails(string Email)
        {
            User user = UserContext.users.Find(Email);
            if (user == null)
                return RedirectToAction("CreateUserDetails",  new { Email = Email });
            return View(user); 
        }
        public ActionResult EditUser(string Email)
        {
            User user = UserContext.users.Find(Email);
            if(user == null)
            {
                return RedirectToAction("CreateUserDetails", new { Email = Email });
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser([Bind(Include = "Email,FirstName,LastName,Dob")]User model)
        {
            if (ModelState.IsValid)
            {
                UserContext.users.Add(model);
                UserContext.SaveChanges();
                return RedirectToAction("UserDetails");
            }
            return View(model);

        }

        public ActionResult AddRoles(string Email)
        {
            Initialize();
            ViewBag.roles = roleManager.Roles.Select(m => m.Name).ToList();
            ViewBag.Email = Email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddRoles([Bind(Include = "Email,Roles")] UserRoles model)
        {

            if (!ModelState.IsValid)
            {
                return View();
            }

            
            
            Initialize();
            var user = manager.FindByEmail(model.Email);
            var roles = manager.GetRoles(user.Id);
            
            foreach(var role in roles)
            {
                model.Roles.Remove(role);
            }
            await manager.AddToRolesAsync(userId: user.Id, model.Roles.ToArray());
            return RedirectToAction("ListUsers");
        }


        public ActionResult RemoveRoles(string Email)
        {
            Initialize();
            var user = manager.FindByEmail(Email);
            ViewBag.roles = manager.GetRoles(user.Id);
            ViewBag.Email = Email;
            return View();
        }

        [ActionName("RemoveRoles")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> RemoveRoles([Bind(Include = "Email,Roles")] UserRoles model)
        {
            if(!ModelState.IsValid)
                return View();
            
            Initialize();
            var user = manager.FindByEmail(model.Email);
            await  manager.RemoveFromRolesAsync(user.Id, model.Roles.ToArray());
            return RedirectToAction("ListUsers");
        }
        

        public ActionResult CreateUserDetails(string Email)
        {
            ViewData["Email"] = Email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUserDetails([Bind(Include = "Email,FirstName,LastName,Dob")]User model)
        {
            if(ModelState.IsValid)
            {
                UserContext.users.Add(model);
                UserContext.SaveChanges();
                return RedirectToAction("ListUsers");
            }
            return View(model);
        }

        public ActionResult ChangePassword(string Email)
        {
            ViewBag.Email = Email;
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword([Bind(Include = "Email,OldPassword,Password,PasswordConfirmation")]ChangePassword model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Initialize();
            var user = manager.FindByEmail(model.Email);
            manager.ChangePassword(user.Id, model.OldPassword, model.Password);
            return RedirectToAction("ListUsers");
        }

        public ActionResult DeleteUser(string Email)
        {
            return View();
        }

        [ActionName("DeleteUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUserConfirmed(string Email)
        {
            Initialize();
            manager.Delete(manager.FindByEmail(Email));
            var user = UserContext.users.Find(Email);
            if(user != null)
                UserContext.users.Remove(user);
            UserContext.SaveChanges();
            return RedirectToAction("ListUsers");
        }



        public ActionResult CreateRole()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateRole(IdentityRole model)
        {

            if(ModelState.IsValid)
            {
                Initialize();
                var code = await roleManager.CreateAsync(model);
                return RedirectToAction("ListRoles");
            }
            
            return View(model); 
        }


        public ActionResult DeleteRole(string roles)
        {
            Initialize();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeleteRole")]
        public async Task<ActionResult> DeleteRoleConfirmed(string roles)
        {
            Initialize();
            await roleManager.DeleteAsync(roleManager.FindByName(roles));
            return RedirectToAction("ListRoles");
        }

        public async Task<ActionResult> EditRole(string roles)
        {
            Initialize();
            IdentityRole model = await roleManager.FindByNameAsync(roles);
            
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> EditRole(IdentityRole model)
        {
            Initialize();
            if (ModelState.IsValid)
            {
                var result = await roleManager.UpdateAsync(model);
                if (result.Succeeded)
                    return RedirectToAction("ListRoles");
            }
            return View(model);
        }

        public ActionResult ListRoles()
        {
            Initialize();
            return View(roleManager.Roles);
        }

        #region Helpers
        private void AddError(IdentityResult result)
        {
            foreach(string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }

        }

        private void Initialize()
        {
            if (initialized == true)
                return;
            context = ApplicationDbContext.Create();
            manager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
        }
        #endregion
    }
}