// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rdmp.Core.CommandLine.DatabaseCreation;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;

namespace Tests.Common.Performance;

/// <summary>
/// Provides a pool of shared repository instances to improve test performance by eliminating
/// repeated repository creation and initialization overhead.
///
/// This class maintains thread-safe singleton instances of repositories that can be reused
/// across test classes while maintaining proper isolation and cleanup.
/// </summary>
public class RepositoryPool : IDisposable
{
    private static readonly Lazy<RepositoryPool> _instance = new(() => new RepositoryPool());
    private static readonly object _lock = new();

    private readonly ConcurrentDictionary<string, RepositoryInstance> _repositories = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private bool _disposed;

    /// <summary>
    /// Gets the singleton instance of the repository pool.
    /// </summary>
    public static RepositoryPool Instance => _instance.Value;

    private RepositoryPool()
    {
        // Private constructor for singleton pattern
    }

    /// <summary>
    /// Gets or creates a repository instance based on the provided options.
    /// </summary>
    /// <param name="options">The database creation options for the repository</param>
    /// <param name="useFileSystemRepo">Whether to use a file system repository instead of database</param>
    /// <returns>A repository locator instance</returns>
    public IRDMPPlatformRepositoryServiceLocator GetOrCreateRepository(PlatformDatabaseCreationOptions options, bool useFileSystemRepo = false)
    {
        var key = GenerateKey(options, useFileSystemRepo);

        var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        semaphore.Wait();
        try
        {
            if (_repositories.TryGetValue(key, out var instance) && !instance.IsDisposed)
            {
                instance.ReferenceCount++;
                return instance.RepositoryLocator;
            }

            // Create new repository instance
            var newLocator = CreateRepositoryLocator(options, useFileSystemRepo);
            var newInstance = new RepositoryInstance(newLocator);

            _repositories.TryAdd(key, newInstance);
            return newLocator;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Releases a reference to a repository instance. When reference count reaches zero,
    /// the repository is scheduled for disposal.
    /// </summary>
    /// <param name="repositoryLocator">The repository locator to release</param>
    public void ReleaseRepository(IRDMPPlatformRepositoryServiceLocator repositoryLocator)
    {
        var key = FindRepositoryKey(repositoryLocator);
        if (key == null) return;

        if (_repositories.TryGetValue(key, out var instance))
        {
            instance.ReferenceCount--;

            // If no more references, dispose after a delay to allow for immediate reuse
            if (instance.ReferenceCount <= 0)
            {
                var delayedTask = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        if (_repositories.TryGetValue(key, out var delayedInstance) && delayedInstance.ReferenceCount <= 0)
                        {
                            delayedInstance.Dispose();
                            _repositories.TryRemove(key, out _);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle exception as appropriate for test utility
                        Console.Error.WriteLine($"Exception during delayed repository disposal: {ex}");
                    }
                });
            }
        }
    }

    /// <summary>
    /// Clears all repository instances and disposes them. Use this in test teardown
    /// to ensure clean state between test runs.
    /// </summary>
    public void ClearAll()
    {
        foreach (var instance in _repositories.Values)
        {
            instance.Dispose();
        }
        _repositories.Clear();

        foreach (var semaphore in _semaphores.Values)
        {
            semaphore.Dispose();
        }
        _semaphores.Clear();
    }

    /// <summary>
    /// Gets statistics about the repository pool for monitoring and debugging.
    /// </summary>
    /// <returns>Dictionary containing pool statistics</returns>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            ["TotalRepositories"] = _repositories.Count,
            ["ActiveRepositories"] = _repositories.Values.Count(r => !r.IsDisposed && r.ReferenceCount > 0),
            ["DisposedRepositories"] = _repositories.Values.Count(r => r.IsDisposed),
            ["TotalReferences"] = _repositories.Values.Sum(r => r.ReferenceCount)
        };
    }

    private static string GenerateKey(PlatformDatabaseCreationOptions options, bool useFileSystemRepo)
    {
        if (useFileSystemRepo)
            return "FileSystemRepo";

        return $"Server:{options.ServerName}_Prefix:{options.Prefix}_User:{options.Username}";
    }

    private static IRDMPPlatformRepositoryServiceLocator CreateRepositoryLocator(PlatformDatabaseCreationOptions options, bool useFileSystemRepo)
    {
        if (useFileSystemRepo)
        {
            var dir = new System.IO.DirectoryInfo(System.IO.Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "SharedRepo"));

            if (!dir.Exists)
                dir.Create();

            return new RepositoryProvider(new YamlRepository(dir));
        }

        return new PlatformDatabaseCreationRepositoryFinder(options);
    }

    private string FindRepositoryKey(IRDMPPlatformRepositoryServiceLocator repositoryLocator)
    {
        return _repositories.FirstOrDefault(kvp => kvp.Value.RepositoryLocator == repositoryLocator).Key;
    }

    public void Dispose()
    {
        if (_disposed) return;

        ClearAll();
        _disposed = true;
    }

    /// <summary>
    /// Represents a repository instance with reference counting.
    /// </summary>
    private class RepositoryInstance : IDisposable
    {
        public IRDMPPlatformRepositoryServiceLocator RepositoryLocator { get; }
        public int ReferenceCount { get; set; }
        public bool IsDisposed { get; private set; }

        public RepositoryInstance(IRDMPPlatformRepositoryServiceLocator repositoryLocator)
        {
            RepositoryLocator = repositoryLocator;
            ReferenceCount = 1;
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            if (RepositoryLocator is IDisposable disposable)
            {
                disposable.Dispose();
            }

            IsDisposed = true;
        }
    }
}