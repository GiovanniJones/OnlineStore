using EC.Models;
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
using System.Net;

namespace EC.Controllers 
{
    [Authorize(Roles = "Administrator,Employee,Manager")]
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
            Session["User_roles"] = roles; 
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
                    await manager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
                    return RedirectToAction("ListUsers");
                }
                AddError(roleResult);
                AddError(result);             
            }
            
            return View(model);
        }

        
        public ActionResult ListUsers(string SortOrder, int? PageNumber, string Search)
        {
            Initialize();
            ViewBag.CurrentSort = SortOrder;
            ViewBag.SortOrder = string.IsNullOrEmpty(SortOrder)? "Descending": string.Empty;
            ViewBag.Search = Search;
            var users = from s in manager.Users select s; 
            if(!string.IsNullOrEmpty(Search))
            {
                users = users.Where(m => m.Email.Contains(Search));
                PageNumber = 1;
            }
            IQueryable<UserView> user;
            switch (SortOrder)
            {
                case "Descending":
                   user =   users.Select(m => new UserView { Email = m.Email, Phone = m.PhoneNumber, UserName = m.UserName }).OrderByDescending(s => s.Email);
                break;
                
                default:
                    user = users.Select(m => new UserView { Email = m.Email, Phone = m.PhoneNumber, UserName = m.UserName }).OrderBy(s => s.Email);
                break;
            }
            int page = (PageNumber ?? 1);
            return View(user.ToPagedList(page, PageSize));
        }

        public  async Task<ActionResult> UserDetails(string Email)
        {
            User user = await UserContext.users.FindAsync(Email);
            if (user == null)
                return RedirectToAction("CreateUserDetails",  new { Email = Email });
            return View(user); 
        }
        public async Task<ActionResult> EditUser(string Email)
        {
            User user = await UserContext.users.FindAsync(Email);
            if(user == null)
            {
                return RedirectToAction("CreateUserDetails", new { Email = Email });
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditUser([Bind(Include = "Email,FirstName,LastName,Dob")]User model)
        {
            if (ModelState.IsValid)
            {
                UserContext.users.Add(model);
                await  UserContext.SaveChangesAsync();
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


        public async Task<ActionResult> RemoveRoles(string Email)
        {
            Initialize();
            var user = await manager.FindByEmailAsync(Email);
            ViewBag.roles = await manager.GetRolesAsync(user.Id);
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
            var user = await manager.FindByEmailAsync(model.Email);
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
        public async Task<ActionResult> CreateUserDetails([Bind(Include = "Email,FirstName,LastName,Dob")]User model)
        {
            
            if(ModelState.IsValid)
            {
                if(User.Identity.Name == model.Email)
                    Session["User"] = model; 

                UserContext.users.Add(model);
                await UserContext.SaveChangesAsync();
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
        public async Task<ActionResult> ChangePassword([Bind(Include = "Email,OldPassword,Password,PasswordConfirmation")]ChangePassword model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Initialize();
            var user = await manager.FindByEmailAsync(model.Email);
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
        public async Task<ActionResult> DeleteUserConfirmed(string Email)
        {
            Initialize();
            manager.Delete(await manager.FindByEmailAsync(Email));
            var user = await UserContext.users.FindAsync(Email);
            if(user != null)
                UserContext.users.Remove(user);
            UserContext.SaveChanges();
            Session["User"] = null;
            
            if (User.Identity.Name == Email)
            {  
                HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie); 
                return RedirectToAction("Index", "Home");
            } 


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

        public ActionResult ListRoles(string SortOrder, int? PageNumber, string Search)
        {
            Initialize();
            ViewBag.CurrentSort = SortOrder;
            ViewBag.SortOrder = string.IsNullOrEmpty(SortOrder) ? "Descending" : string.Empty;
            ViewBag.Search = Search;
            var roles = from s in roleManager.Roles select s; 

            if(!string.IsNullOrEmpty(Search))
            {
                roles = roles.Where(m => m.Name.Contains(Search));
                PageNumber = 1; 
            }

            switch(SortOrder)
            {
                case "Descending":
                    roles = roles.OrderByDescending(m => m.Name);
                    break;
                default:
                    roles = roles.OrderBy(m => m.Name);
                    break; 
            }
            int page = (PageNumber ?? 1);
            return View(roles.ToPagedList(page, PageSize));
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