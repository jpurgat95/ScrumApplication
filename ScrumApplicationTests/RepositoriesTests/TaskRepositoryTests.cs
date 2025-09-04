using Xunit;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using System.Linq;
namespace ScrumApplicationTests
{
    public class TaskRepositoryTests
    {
        private ScrumDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ScrumDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            return new ScrumDbContext(options);
        }

        [Fact]
        public async Task AddTaskAsync_ShouldAddTask()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);
            var user = new IdentityUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);

            var task = new TaskItem
            {
                Id = 1,
                Title = "Test Task",
                UserId = user.Id,
                User = user,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
            };

            await context.SaveChangesAsync();

            await repo.AddTaskAsync(task);

            var addedTask = await context.Tasks.FindAsync(1);
            Assert.NotNull(addedTask);
            Assert.Equal("Test Task", addedTask.Title);
            Assert.Equal(user.Id, addedTask.UserId);
        }

        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_ForAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);

            var user = new IdentityUser { Id = "user2", UserName = "adminuser" };
            context.Users.Add(user);
            var testEvent = new ScrumEvent
            {
                Id = 1,
                Title = "Test Event",
                UserId = user.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                User = user
            };
            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var task = new TaskItem
            {
                Id = 2,
                Title = "Admin Task",
                UserId = user.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                User = user,
                ScrumEvent = testEvent
            };

            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var result = await repo.GetTaskByIdAsync(2, "anyUserId", isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal("Admin Task", result.Title);
            Assert.NotNull(result.User);
            Assert.Equal("adminuser", result.User.UserName);
        }

        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_ForOwner()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);

            var user = new IdentityUser { Id = "user3", UserName = "normaluser" };
            context.Users.Add(user);
            var testEvent = new ScrumEvent
            {
                Id = 1,
                Title = "Test Event",
                UserId = user.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                User = user
            };
            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var task = new TaskItem
            {
                Id = 3,
                Title = "User Task",
                UserId = user.Id,
                User = user,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                ScrumEvent = testEvent
            };

            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var result = await repo.GetTaskByIdAsync(3, user.Id, isAdmin: false);

            Assert.NotNull(result);
            Assert.Equal("User Task", result.Title);
            Assert.NotNull(result.User);
            Assert.Equal("normaluser", result.User.UserName);
        }

        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnNull_ForNonOwnerNonAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);

            var user = new IdentityUser { Id = "user4", UserName = "userA" };
            context.Users.Add(user);

            var task = new TaskItem
            {
                Id = 4,
                Title = "Hidden Task",
                UserId = user.Id,
                User = user,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
            };

            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var result = await repo.GetTaskByIdAsync(4, "differentUser", isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetTasksAsync_ShouldReturnAllTasks_ForAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);

            var user1 = new IdentityUser { Id = "user1", UserName = "UserOne" };
            var user2 = new IdentityUser { Id = "user2", UserName = "UserTwo" };
            context.Users.AddRange(user1, user2);
            var event1 = new ScrumEvent { Id = 1, Title = "Event 1", UserId = user1.Id, StartDate = DateTime.Now, User = user1 };
            var event2 = new ScrumEvent { Id = 2, Title = "Event 2", UserId = user2.Id, StartDate = DateTime.Now.AddHours(1), User = user2 };
            context.Events.AddRange(event1, event2);
            var task1 = new TaskItem { Id = 5, Title = "Task 1", UserId = user1.Id, User = user1, StartDate = DateTime.Now, ScrumEvent = event1 };
            var task2 = new TaskItem { Id = 6, Title = "Task 2", UserId = user2.Id, User = user2, StartDate = DateTime.Now.AddHours(1), ScrumEvent = event2 };
            context.Tasks.AddRange(task1, task2);

            await context.SaveChangesAsync();

            var result = await repo.GetTasksAsync("anyUserId", true);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Title == "Task 1");
            Assert.Contains(result, t => t.Title == "Task 2");
            Assert.All(result, t => Assert.NotNull(t.User));
        }

        [Fact]
        public async Task GetTasksAsync_ShouldReturnOnlyUserTasks_WhenNotAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);

            var user1 = new IdentityUser { Id = "user1", UserName = "UserOne" };
            var user2 = new IdentityUser { Id = "user2", UserName = "UserTwo" };
            context.Users.AddRange(user1, user2);

            var event1 = new ScrumEvent { Id = 1, Title = "Event 1", UserId = user1.Id, StartDate = DateTime.Now, User = user1 };
            var event2 = new ScrumEvent { Id = 2, Title = "Event 2", UserId = user2.Id, StartDate = DateTime.Now.AddHours(1), User = user2 };
            context.Events.AddRange(event1, event2);

            var task1 = new TaskItem { Id = 7, Title = "Task 1", UserId = user1.Id, User = user1, StartDate = DateTime.Now, ScrumEvent = event1 };
            var task2 = new TaskItem { Id = 8, Title = "Task 2", UserId = user2.Id, User = user2, StartDate = DateTime.Now.AddHours(1), ScrumEvent = event2 };
            context.Tasks.AddRange(task1, task2);

            await context.SaveChangesAsync();

            var result = await repo.GetTasksAsync(user1.Id, false);

            Assert.Single(result);
            Assert.Equal("Task 1", result[0].Title);
            Assert.NotNull(result[0].User);
            Assert.Equal("UserOne", result[0].User.UserName);
        }

        [Fact]
        public async Task GetTasksByEventIdAsync_ShouldReturnTasksForEvent()
        {
            var context = GetInMemoryContext();
            var repo = new TaskRepository(context);

            var user = new IdentityUser { Id = "userX", UserName = "userX" };
            context.Users.Add(user);

            var eventItem = new ScrumEvent { Id = 100, Title = "Event 1", StartDate = DateTime.Now, User = user };
            context.Events.Add(eventItem);

            var task1 = new TaskItem
            {
                Id = 10,
                Title = "Task 1",
                UserId = user.Id,
                User = user,
                ScrumEventId = 100,
                ScrumEvent = eventItem,
                StartDate = DateTime.Now,
            };
            var task2 = new TaskItem
            {
                Id = 11,
                Title = "Task 2",
                UserId = user.Id,
                User = user,
                ScrumEventId = 100,
                ScrumEvent = eventItem,
                StartDate = DateTime.Now.AddMinutes(10)
            };
            context.Tasks.AddRange(task1, task2);

            await context.SaveChangesAsync();

            var result = await repo.GetTasksByEventIdAsync(100);

            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.NotNull(t.User));
            Assert.All(result, t => Assert.NotNull(t.ScrumEvent));
            Assert.All(result, t => Assert.Equal(100, t.ScrumEventId));
        }
    }
}