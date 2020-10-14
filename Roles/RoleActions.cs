using EC.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EC.Roles
{
    public class RoleActions
    {
        ApplicationDbContext dBContext;
        IdentityResult IdRoleResult;
        IdentityResult IdUserResult;
        RoleStore<IdentityRole> roleStore;
        RoleManager<IdentityRole> roleMgr;

        public RoleActions()
        {
             dBContext = ApplicationDbContext.Create();
             roleStore = new RoleStore<IdentityRole>(dBContext);
             roleMgr = new RoleManager<IdentityRole>(roleStore);
        }

        public void AddUserAndRole()
        {

            var roles = new List<string>
            {
                 "Administrator",
                 "Customer",
                 "Manager",
                 "Employee"
            };

            AddRoles(roles);
            var userMgr = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(dBContext));
            var appUser = new ApplicationUser
            {
                Email = "giovanni_jones23@hotmail.com",
                UserName = "giovanni_jones23@hotmail.com",
            };

            IdUserResult = userMgr.Create(appUser, "Pa$$word1");
            if (IdUserResult.Succeeded && !userMgr.IsInRole(userMgr.FindByEmail("giovanni_jones23@hotmail.com").Id, "Administrator"))
            {
                IdUserResult = userMgr.AddToRole(userMgr.FindByEmail("giovanni_jones23@hotmail.com").Id, "Administrator");
            }
         
        }

        public void AddRoles(List<string> roles)
        { 
            
            
            foreach(string role in roles)
            {
                if(roleMgr.RoleExists(role) == false)
                {
                    IdRoleResult = roleMgr.Create(new IdentityRole { Name = role });
                }
            }
        }
    }
}