using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using ScrumApplication.Pages.Api;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
namespace ScrumApplicationTests
{
    public class EventsModelTests
    {
        private ScrumDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ScrumDbContext>()
                .UseInMemoryDatabase(databaseName: "EventsTestDb_" + System.Guid.NewGuid())
                .Options;
            return new ScrumDbContext(options);
        }

        private EventsModel GetEventsModelWithUser(ScrumDbContext context, string userId, bool isAdmin)
        {
            var model = new EventsModel(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = user };
            model.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = httpContext
            };

            return model;
        }

        [Fact]
        public void OnGet_ShouldReturnAllEvents_ForAdmin()
        {
            var context = GetInMemoryContext();

            var userAdmin = new IdentityUser { Id = "admin1", UserName = "adminUser" };
            var userNormal = new IdentityUser { Id = "user1", UserName = "normalUser" };

            context.Users.AddRange(userAdmin, userNormal);
            context.Events.Add(new ScrumEvent { Id = 1, Title = "Event 1", UserId = userAdmin.Id, User = userAdmin });
            context.Events.Add(new ScrumEvent { Id = 2, Title = "Event 2", UserId = userNormal.Id, User = userNormal });

            context.SaveChanges();

            var model = GetEventsModelWithUser(context, userAdmin.Id, isAdmin: true);

            var result = model.OnGet() as JsonResult;
            Assert.NotNull(result);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jArray = Newtonsoft.Json.Linq.JArray.Parse(json);

            Assert.Equal(2, jArray.Count);
            Assert.Equal("adminUser", jArray[0]["userName"].ToString());
            Assert.Equal("normalUser", jArray[1]["userName"].ToString());
        }

        [Fact]
        public void OnGet_ShouldReturnUserEvents_ForNonAdmin()
        {
            var context = GetInMemoryContext();

            var userNormal = new IdentityUser { Id = "user1", UserName = "normalUser" };
            var otherUser = new IdentityUser { Id = "user2", UserName = "otherUser" };

            context.Users.AddRange(userNormal, otherUser);
            context.Events.Add(new ScrumEvent { Id = 1, Title = "Event 1", UserId = userNormal.Id, User = userNormal });
            context.Events.Add(new ScrumEvent { Id = 2, Title = "Event 2", UserId = otherUser.Id, User = otherUser });

            context.SaveChanges();

            var model = GetEventsModelWithUser(context, userNormal.Id, isAdmin: false);

            var result = model.OnGet() as JsonResult;
            Assert.NotNull(result);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jArray = Newtonsoft.Json.Linq.JArray.Parse(json);

            Assert.Single(jArray);
            Assert.True(string.IsNullOrEmpty((string)jArray[0]["userName"]));
            Assert.Equal("Event 1", jArray[0]["title"].ToString());
        }
        [Fact]
        public void OnPostToggleDone_AdminCanToggleAnyEvent()
        {
            var context = GetInMemoryContext();

            var adminUser = new IdentityUser { Id = "admin1", UserName = "admin" };
            var normalUser = new IdentityUser { Id = "user1", UserName = "user" };

            context.Users.AddRange(adminUser, normalUser);
            var ev = new ScrumEvent { Id = 1, Title = "Event 1", UserId = normalUser.Id, IsDone = false };
            context.Events.Add(ev);
            context.SaveChanges();

            var model = GetEventsModelWithUser(context, adminUser.Id, true);

            var result = model.OnPostToggleDone(1) as JsonResult;
            Assert.NotNull(result);

            dynamic json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(json);

            Assert.True((bool)jsonObj["success"]);
            Assert.True((bool)jsonObj["isDone"]);
        }

        [Fact]
        public void OnPostToggleDone_UserCanToggleOwnEvent()
        {
            var context = GetInMemoryContext();

            var normalUser = new IdentityUser { Id = "user1", UserName = "user" };
            context.Users.Add(normalUser);
            var ev = new ScrumEvent { Id = 2, Title = "Event 2", UserId = normalUser.Id, IsDone = false };
            context.Events.Add(ev);
            context.SaveChanges();

            var model = GetEventsModelWithUser(context, normalUser.Id, false);

            var result = model.OnPostToggleDone(2) as JsonResult;
            Assert.NotNull(result);

            dynamic json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(json);

            Assert.True((bool)jsonObj["success"]);
            Assert.True((bool)jsonObj["isDone"]);
        }

        [Fact]
        public void OnPostToggleDone_UserCannotToggleOthersEvent()
        {
            var context = GetInMemoryContext();

            var userA = new IdentityUser { Id = "userA", UserName = "userA" };
            var userB = new IdentityUser { Id = "userB", UserName = "userB" };

            context.Users.AddRange(userA, userB);
            var ev = new ScrumEvent { Id = 3, Title = "Event 3", UserId = userA.Id, IsDone = false };
            context.Events.Add(ev);
            context.SaveChanges();

            var model = GetEventsModelWithUser(context, userB.Id, false);

            var result = model.OnPostToggleDone(3) as JsonResult;
            Assert.NotNull(result);

            dynamic json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(json);

            Assert.False((bool)jsonObj["success"]);
        }
    }
}