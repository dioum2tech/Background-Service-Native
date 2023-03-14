using Background_Service_Native.Jobs;
using Background_Service_Native.Schedulers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCrontab;

namespace Background_Service_Native.Infrastructure.Configurations
{
    public static class CronJobConfiguration
    {
        public static IServiceCollection AddCronJob<T>(this IServiceCollection services, string cronExpression) where T : class, ICronJob
        {
            var cron = CrontabSchedule.TryParse(cronExpression)
                       ?? throw new ArgumentException("Invalid cron expression", nameof(cronExpression));

            var entry = new CronRegistryEntry(typeof(T), cron);

            services.AddHostedService<CronScheduler>();
            services.TryAddSingleton<T>();
            services.AddSingleton(entry);

            return services;
        }
    }
}
