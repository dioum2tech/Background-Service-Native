using NCrontab;

namespace Background_Service_Native.Infrastructure.Configurations;

public sealed record CronRegistryEntry(Type Type, CrontabSchedule CrontabSchedule);
