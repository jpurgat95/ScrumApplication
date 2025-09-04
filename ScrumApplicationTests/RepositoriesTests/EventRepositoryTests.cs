using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace ScrumApplicationTests
{
    public class EventRepositoryTests
    {
        private ScrumDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ScrumDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + System.Guid.NewGuid())
                .Options;
            return new ScrumDbContext(options);
        }

        [Fact]
        public async Task AddEventAsync_ShouldAddEvent()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            var newEvent = new ScrumEvent { Id = 1, Title = "Test event", UserId = "user1", StartDate = System.DateTime.Now, EndDate = System.DateTime.Now.AddHours(1) };

            // Act
            await repo.AddEventAsync(newEvent);

            // Assert
            var addedEvent = await context.Events.FindAsync(1);
            Assert.NotNull(addedEvent);
            Assert.Equal("Test event", addedEvent.Title);
        }

        [Fact]
        public async Task DeleteEventAsync_ShouldRemoveEvent()
        {
            // Arrange
            var context = GetInMemoryContext();
            var eventToDelete = new ScrumEvent { Id = 1, Title = "To delete", UserId = "user1", StartDate = System.DateTime.Now };
            context.Events.Add(eventToDelete);
            await context.SaveChangesAsync();

            var repo = new EventRepository(context);

            // Act
            await repo.DeleteEventAsync(eventToDelete);

            // Assert
            var deletedEvent = await context.Events.FindAsync(1);
            Assert.Null(deletedEvent);
        }

        [Fact]
        public async Task UpdateEventAsync_ShouldModifyEvent()
        {
            var options = new DbContextOptionsBuilder<ScrumDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_UpdateEvent")
                .Options;

            using (var context = new ScrumDbContext(options))
            {
                context.Events.Add(new ScrumEvent { Id = 4, Title = "Old Title", UserId = "user1", StartDate = DateTime.Now });
                await context.SaveChangesAsync();
            }

            using (var context = new ScrumDbContext(options))
            {
                var repo = new EventRepository(context);
                var eventToUpdate = await context.Events.FindAsync(4);
                eventToUpdate.Title = "Updated Title";

                await repo.UpdateEventAsync(eventToUpdate);
            }

            using (var context = new ScrumDbContext(options))
            {
                var updatedEvent = await context.Events.FindAsync(4);
                Assert.Equal("Updated Title", updatedEvent.Title);
            }
        }

        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnEvent_WhenEventExistsAndUserIsAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new EventRepository(context);

            // Dodajemy użytkownika, którego UserId będzie powiązane z eventem
            var testUser = new IdentityUser { Id = "someUserId", UserName = "testuser" };
            context.Users.Add(testUser);

            var testEvent = new ScrumEvent
            {
                Id = 1,
                Title = "Test Event",
                UserId = testUser.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                User = testUser // Można też od razu przypisać obiekt User
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var result = await repo.GetEventByIdAsync(1, "anyUserId", isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal("Test Event", result.Title);
            Assert.NotNull(result.User);
            Assert.Equal("testuser", result.User.UserName);
        }

        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnNull_WhenUserIsNotOwnerAndNotAdmin()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repo = new EventRepository(context);

            var testEvent = new ScrumEvent
            {
                Id = 2,
                Title = "User Event",
                UserId = "user1",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1)
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetEventByIdAsync(2, "otherUser", isAdmin: false);

            // Assert
            Assert.Null(result);
        }
        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnEvent_WhenUserIsOwnerAndNotAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new EventRepository(context);

            // Dodaj użytkownika i event powiązany z tym userId
            var testUser = new IdentityUser { Id = "user123", UserName = "normaluser" };
            context.Users.Add(testUser);

            var testEvent = new ScrumEvent
            {
                Id = 10,
                Title = "User's Event",
                UserId = testUser.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                User = testUser
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            // Wywołanie metody repozytorium - nie jest adminem i przekazuje swój userId
            var result = await repo.GetEventByIdAsync(10, "user123", isAdmin: false);

            Assert.NotNull(result);
            Assert.Equal("User's Event", result.Title);
            Assert.NotNull(result.User);
            Assert.Equal("normaluser", result.User.UserName);
        }
        [Fact]
        public async Task GetEventsAsync_ShouldReturnAllEvents_WhenUserIsAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new EventRepository(context);

            // Dodajemy użytkownika i 2 eventy przypisane do różnych userId
            var user1 = new IdentityUser { Id = "user1", UserName = "UserOne" };
            var user2 = new IdentityUser { Id = "user2", UserName = "UserTwo" };
            context.Users.AddRange(user1, user2);

            var event1 = new ScrumEvent { Id = 1, Title = "Event 1", UserId = user1.Id, StartDate = DateTime.Now, User = user1 };
            var event2 = new ScrumEvent { Id = 2, Title = "Event 2", UserId = user2.Id, StartDate = DateTime.Now.AddHours(1), User = user2 };
            context.Events.AddRange(event1, event2);

            await context.SaveChangesAsync();

            // Wywołanie metody jako admin - powinien zobaczyć wszystkie eventy
            var result = await repo.GetEventsAsync("anyUserId", true);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Title == "Event 1");
            Assert.Contains(result, e => e.Title == "Event 2");
            Assert.All(result, e => Assert.NotNull(e.User)); // Sprawdzamy Include User
        }

        [Fact]
        public async Task GetEventsAsync_ShouldReturnOnlyUserEvents_WhenUserIsNotAdmin()
        {
            var context = GetInMemoryContext();
            var repo = new EventRepository(context);

            // Dodajemy użytkownika i eventy związane z różnymi userId
            var user1 = new IdentityUser { Id = "user1", UserName = "UserOne" };
            var user2 = new IdentityUser { Id = "user2", UserName = "UserTwo" };
            context.Users.AddRange(user1, user2);

            var event1 = new ScrumEvent { Id = 1, Title = "Event 1", UserId = user1.Id, StartDate = DateTime.Now, User = user1 };
            var event2 = new ScrumEvent { Id = 2, Title = "Event 2", UserId = user2.Id, StartDate = DateTime.Now.AddHours(1), User = user2 };
            context.Events.AddRange(event1, event2);

            await context.SaveChangesAsync();

            // Wywołanie metody jako zwykły użytkownik user1 - powinien zobaczyć tylko swoje eventy
            var result = await repo.GetEventsAsync("user1", false);

            Assert.Single(result);
            Assert.Equal("Event 1", result[0].Title);
            Assert.NotNull(result[0].User);
            Assert.Equal("UserOne", result[0].User.UserName);
        }
    }
}