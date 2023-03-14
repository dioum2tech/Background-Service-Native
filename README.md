# [BackgroundService](https://steven-giesel.com/blogPost/fb1ce2ab-dd27-43ed-aaab-077adf2d15cd)
How to write your own cron Job scheduler in ASP.NET Core (like Quartz, Hangfire, ...)

## How to write your own cron Job scheduler in ASP.NET Core (like Quartz, Hangfire, ...)

The first question we should answer before coding is "What is a BackgroundService?". A BackgroundService is a long-running service running in the background of an ASP.NET Core application. It is typically used to perform database maintenance, sending emails, or processing messages from a message queue.

A BackgroundService is implemented by creating a class that inherits from the BackgroundService base class and overrides the ExecuteAsync method. The `ExecuteAsync` method is where the code to perform the long-running task should be written. The BackgroundService base class takes care of starting and stopping the service and handling any exceptions that may occur during the execution of the service.

BackgroundServices can be registered with the ASP.NET Core dependency injection system, allowing them to be easily integrated into the application. They can also be hosted in various ways, including as a standalone console application or as part of an ASP.NET Core web application.

`BackgroundServices` are registered singleton! And that is vital to understand because it isn't called background without reason. As such, you don't have any `HttpContext` around. Also, the service itself is registered as a singleton. So retrieving scoped services as a constructor dependency does not work out of the box.

## Usage
I want to have a very easy entry for the user to register services. Something like this:

```
builder.Services.AddCronJob<CronJob>("* * * * *");
builder.Services.AddCronJob<AnotherCronJob>("*/2 * * * *");
```
The only parameter we have is the cron expression. Now for the sake of simplicity, the cron expression will always be evaluated to UTC dates. In a real-world example, you might want to introduce time zone information as well.

How does the `AddCronJob` function look like?

```
public static IServiceCollection AddCronJob<T>(this IServiceCollection services, string cronExpression)
    where T : class, ICronJob
{
    var cron = CrontabSchedule.TryParse(cronExpression)
               ?? throw new ArgumentException("Invalid cron expression", nameof(cronExpression));

    var entry = new CronRegistryEntry(typeof(T), cron);

    services.AddHostedService<CronScheduler>();
    services.TryAddSingleton<T>();
    services.AddSingleton(entry);

    return services;
}
```
First, we can see it is an extension method. It wants a `T` that inherits from `ICronJob`. So every job that we want to execute has to implement said interface. But the interface is very slim:

```
public interface ICronJob
{
    Task Run(CancellationToken token = default);
}
```
Back to our extension method. We have a `CrontabSchedule.TryParse(cronExpression)` here. This method comes from the NCronTab library that does all the heavy lifting in terms of cron parsing for us. If we can't parse the expression, we throw an exception.

The next line (`var entry = new CronRegistryEntry(typeof(T), cron)`) creates a new object. The object is defined as follows:

```
public sealed record CronRegistryEntry(Type Type, CrontabSchedule CrontabSchedule);
```
Basically, we are creating an entry in a registry we can pick up later. So each of those entries is one job linked to a cron expression. We are separating all those things so that our ICronJob itself doesn't have to know anything about a scheduler or a cron notation in the first place. It also keeps our system extendable. I will make this more clear in the outlook part.

Now the next three lines might be confusing:

```
services.AddHostedService<CronScheduler>();
services.TryAddSingleton<T>();
services.AddSingleton(entry);
```