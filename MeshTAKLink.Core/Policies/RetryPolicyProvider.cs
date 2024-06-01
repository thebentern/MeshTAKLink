﻿using Microsoft.Extensions.Logging;
using Polly;

namespace MeshTAKLink.Core.Policies;

public static class RetryPolicyProvider
{
    public static async Task GetDeviceConnectionRetryPolicy(ILogger logger, Func<Task> operation, Func<Exception, TimeSpan, Task> onRetryAsync) => await Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(Int32.MaxValue, (_) => TimeSpan.FromSeconds(1), onRetryAsync)
        .ExecuteAsync(operation);
}