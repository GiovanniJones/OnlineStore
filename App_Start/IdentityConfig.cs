using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using EC.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Configuration;
using System.Diagnostics;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

namespace EC
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            return configSendGridasync(message);
        }

        private async Task<Response> configSendGridasync(IdentityMessage message)
        {

            var apiKey = ConfigurationManager.AppSettings["SGK"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("giovani_jones23@hotmail.com", "Giovanni Jones");
            var subject = message.Subject; 
            var to = new EmailAddress(message.Destination, "Valued Customer");
            var plainTextContent = message.Body;
            var htmlContent = message.Body;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            Response response = null; 
           
            if (client != null)
                return response = await client.SendEmailAsync(msg);
            else 
            {
                Trace.TraceError("Failed to create Web transport.");
                return response; 
            }
            
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public  Task SendAsync(IdentityMessage message)
        {


            var soap = new ASPSMSX2.ASPSMSX2SoapClient("ASPSMSX2Soap");
            if (soap != null)
            {
                soap.SendSimpleTextSMS(
                System.Configuration.ConfigurationManager.AppSettings["ASPSMSUSERKEY"],
                System.Configuration.ConfigurationManager.AppSettings["ASPSMSPASSWORD"],
                message.Destination,
                System.Configuration.ConfigurationManager.AppSettings["ASPSMSORIGINATOR"],
                message.Body);
                soap.Close();

            }
            return Task.FromResult(0);
                      
        }

    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                    manager.UserTokenProvider =
                            new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity")) { TokenLifespan = TimeSpan.FromHours(3) };
            }
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
