using JoyOI.UserCenter.SDK;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddJoyOIUserCenter(this IServiceCollection self)
        {
            return self.AddSingleton<UserCenter>();
        }
    }
}
