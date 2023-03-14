namespace Background_Service_Native.Jobs;

public interface ICronJob
{
    Task Run(CancellationToken token = default);
}
