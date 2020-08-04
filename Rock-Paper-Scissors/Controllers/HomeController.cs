using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Rock_Paper_Scissors.Data;
using Rock_Paper_Scissors.Hubs;
using Rock_Paper_Scissors.Models;

namespace Rock_Paper_Scissors.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        readonly AppDbContext _context;
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index() // main page
        {
            if(User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Play");
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Play()
        {
            var result = await _context.FindAsync<UserModel>(User.Identity.Name);
            if(result != null)
            {
                return View(result);
            }
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Index(UserModel model)
        {
            if(ModelState.IsValid)
            {
                // checking if there is another account with the same username

                var result = await _context.FindAsync<UserModel>(model.UserName);

                if(result != null)
                {
                    if(GameHub.IsOnline(result.UserName))
                    {
                        ModelState.AddModelError(string.Empty, "Username already taken");

                        return View(model);
                    }
                    else
                    {
                        await HttpContext.SignInAsync(SetupClaims(model));

                        return RedirectToAction("Play");
                    }
                }

                // setting up the claims

                var user = SetupClaims(model);

                await HttpContext.SignInAsync(user);

                await _context.AddAsync(model);

                await _context.SaveChangesAsync();

                return RedirectToAction("Play");
            }
            return View();
        }

        ClaimsPrincipal SetupClaims(UserModel model)
        {
            var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, model.UserName)
                };

            var identity = new ClaimsIdentity(claims, "Claims Identity");

            var user = new ClaimsPrincipal(new[] { identity });

            return user;
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            // getting the model from context by the identity
            var model = await _context.FindAsync<UserModel>(User.Identity.Name);
            if(model != null)
            {// then we found it
                await _context.SaveChangesAsync(); // saving any possible unsaved change
            }
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
