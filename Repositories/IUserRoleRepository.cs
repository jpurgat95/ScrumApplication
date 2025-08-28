public interface IUserRoleRepository
{
    Task<List<string>> GetUserIdsInRoleAsync(string roleId);
    Task<List<string>> GetUserIdsNotInRolesAsync(List<string> excludedRoleUserIds);
}
