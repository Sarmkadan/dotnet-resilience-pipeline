// Copyright (c) DotNetResiliencePipeline. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Extension methods for <see cref="ResiliencyPolicy"/> collections.
/// </summary>
public static class ResiliencyPolicyEnumerableExtensions
{
    /// <summary>
    /// Filters a collection of policies to only include those that are enabled.
    /// </summary>
    /// <param name="policies">The collection of policies to filter.</param>
    /// <returns>An enumerable containing only the enabled policies.</returns>
    public static IEnumerable<ResiliencyPolicy> WhereEnabled(this IEnumerable<ResiliencyPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        return policies.Where(p => p.IsEnabled);
    }
}
