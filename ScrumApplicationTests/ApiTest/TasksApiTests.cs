using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ScrumApplication.Data;
using ScrumApplication.Models;
using ScrumApplication.Pages.Api;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
namespace ScrumApplicationTests
{
    public class TasksApiTests
    {
        private ScrumDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ScrumDbContext>()
                .UseInMemoryDatabase("TestDb_" + System.Guid.NewGuid())
                .Options;
            return new ScrumDbContext(options);
        }

        private TasksModel GetTasksModelWithUser(ScrumDbContext context, string userId, bool isAdmin)
        {
            var model = new TasksModel(context);
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            model.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext()
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return model;
        }

        [Fact]
        public void OnGet_ShouldReturnAllTasks_ForAdmin()
        {
            var context = GetInMemoryContext();

            var adminUser = new IdentityUser { Id = "admin1", UserName = "adminUser" };
            var user1 = new IdentityUser { Id = "user1", UserName = "user1" };

            context.Users.AddRange(adminUser, user1);

            context.Tasks.Add(new TaskItem { Id = 1, Title = "Task 1", UserId = adminUser.Id, User = adminUser, StartDate = System.DateTime.Now });
            context.Tasks.Add(new TaskItem { Id = 2, Title = "Task 2", UserId = user1.Id, User = user1, StartDate = System.DateTime.Now.AddHours(1) });

            context.SaveChanges();

            var model = GetTasksModelWithUser(context, adminUser.Id, true);

            var result = model.OnGet() as JsonResult;
            Assert.NotNull(result);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jArray = JArray.Parse(json);

            Assert.Equal(2, jArray.Count);
            Assert.Equal("adminUser", jArray[0]["userName"].ToString());
            Assert.Equal("user1", jArray[1]["userId"].ToString());
        }

        [Fact]
        public void OnGet_ShouldReturnOnlyUserTasks_ForNonAdmin()
        {
            var context = GetInMemoryContext();

            var user1 = new IdentityUser { Id = "user1", UserName = "user1" };
            var user2 = new IdentityUser { Id = "user2", UserName = "user2" };

            context.Users.AddRange(user1, user2);

            context.Tasks.Add(new TaskItem { Id = 1, Title = "Task 1", UserId = user1.Id, User = user1, StartDate = System.DateTime.Now });
            context.Tasks.Add(new TaskItem { Id = 2, Title = "Task 2", UserId = user2.Id, User = user2, StartDate = System.DateTime.Now.AddHours(1) });

            context.SaveChanges();

            var model = GetTasksModelWithUser(context, user1.Id, false);

            var result = model.OnGet() as JsonResult;
            Assert.NotNull(result);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
            var jArray = JArray.Parse(json);

            Assert.Single(jArray);
            Assert.True(string.IsNullOrEmpty((string)jArray[0]["userName"]));
            Assert.Equal("Task 1", jArray[0]["title"].ToString());
        }

        [Fact]
        public void OnPostToggleDone_ShouldToggleTask_ForAuthorizedUser()
        {
            var context = GetInMemoryContext();

            var userAdmin = new IdentityUser { Id = "admin1", UserName = "adminUser" };
            var user1 = new IdentityUser { Id = "user1", UserName = "user1" };

            var task = new TaskItem { Id = 100, Title = "Task", UserId = user1.Id, User = user1, IsDone = false };

            context.Users.AddRange(userAdmin, user1);
            context.Tasks.Add(task);
            context.SaveChanges();

            var modelAdmin = GetTasksModelWithUser(context, userAdmin.Id, true);
            var jsonResultAdmin = modelAdmin.OnPostToggleDone(100) as JsonResult;

            Assert.NotNull(jsonResultAdmin);
            var jsonAdmin = Newtonsoft.Json.JsonConvert.SerializeObject(jsonResultAdmin.Value);
            var jObjAdmin = JObject.Parse(jsonAdmin);
            Assert.True((bool)jObjAdmin["success"]);
            Assert.True((bool)jObjAdmin["isDone"]);

            var modelUser = GetTasksModelWithUser(context, user1.Id, false);
            var jsonResultUser = modelUser.OnPostToggleDone(100) as JsonResult;

            Assert.NotNull(jsonResultUser);
            var jsonUser = Newtonsoft.Json.JsonConvert.SerializeObject(jsonResultUser.Value);
            var jObjUser = JObject.Parse(jsonUser);
            Assert.True((bool)jObjUser["success"]);
            Assert.False((bool)jObjUser["isDone"]);
        }

        [Fact]
        public void OnPostToggleDone_ShouldFail_ForUnauthorizedUser()
        {
            var context = GetInMemoryContext();

            var user1 = new IdentityUser { Id = "user1", UserName = "user1" };
            var user2 = new IdentityUser { Id = "user2", UserName = "user2" };

            var task = new TaskItem { Id = 100, Title = "Task", UserId = user1.Id, User = user1, IsDone = false };

            context.Users.AddRange(user1, user2);
            context.Tasks.Add(task);
            context.SaveChanges();

            var model = GetTasksModelWithUser(context, user2.Id, false);
            var jsonResult = model.OnPostToggleDone(100) as JsonResult;

            Assert.NotNull(jsonResult);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonResult.Value);
            var jObj = JObject.Parse(json);
            Assert.False((bool)jObj["success"]);
        }
    }
}