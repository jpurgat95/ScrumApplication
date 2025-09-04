using Xunit;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
namespace ScrumApplicationTests
{
    public class UserRoleRepositoryTests
    {
        private ScrumDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ScrumDbContext>()
                .UseInMemoryDatabase("TestDb_UserRole_" + System.Guid.NewGuid())
                .Options;
            return new ScrumDbContext(options);
        }

        [Fact]
        public async Task GetUserIdsInRoleAsync_ShouldReturnUserIdsForRole()
        {
            var context = GetInMemoryContext();

            // Dodaj użytkowników i role
            var user1 = new IdentityUser { Id = "user1", UserName = "UserOne" };
            var user2 = new IdentityUser { Id = "user2", UserName = "UserTwo" };
            context.Users.AddRange(user1, user2);

            // Dodaj powiązania użytkowników z rolami
            context.UserRoles.AddRange(
                new IdentityUserRole<string> { UserId = "user1", RoleId = "roleA" },
                new IdentityUserRole<string> { UserId = "user2", RoleId = "roleB" }
            );

            await context.SaveChangesAsync();

            var repo = new UserRoleRepository(context);

            var userIdsInRoleA = await repo.GetUserIdsInRoleAsync("roleA");

            Assert.Single(userIdsInRoleA);
            Assert.Contains("user1", userIdsInRoleA);
        }

        [Fact]
        public async Task GetUserIdsNotInRolesAsync_ShouldReturnUsersNotInExcludedRoles()
        {
            var context = GetInMemoryContext();

            var user1 = new IdentityUser { Id = "user1", UserName = "UserOne" };
            var user2 = new IdentityUser { Id = "user2", UserName = "UserTwo" };
            var user3 = new IdentityUser { Id = "user3", UserName = "UserThree" };
            context.Users.AddRange(user1, user2, user3);

            // Załóżmy, że użytkownicy "user1" i "user2" są w jakiś rolach wykluczonych
            var excludedUserIds = new List<string> { "user1", "user2" };

            await context.SaveChangesAsync();

            var repo = new UserRoleRepository(context);

            var userIdsNotInExcludedRoles = await repo.GetUserIdsNotInRolesAsync(excludedUserIds);

            Assert.Single(userIdsNotInExcludedRoles);
            Assert.Contains("user3", userIdsNotInExcludedRoles);
        }
    }
}