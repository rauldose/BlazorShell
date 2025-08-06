using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Cache for storing module metadata to support reliable reload operations
    /// </summary>
    public class ModuleMetadataCache
    {
        private readonly ConcurrentDictionary<string, ModuleMetadata> _cache = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        public class ModuleMetadata
        {
            public string ModuleName { get; set; } = string.Empty;
            public string AssemblyPath { get; set; } = string.Empty;
            public string AssemblyName { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public DateTime LoadedAt { get; set; }
            public DateTime? UnloadedAt { get; set; }
            public bool IsEnabled { get; set; }
            public bool IsCore { get; set; }
            public Dictionary<string, object> Configuration { get; set; } = new();
            public List<string> Dependencies { get; set; } = new();
            public ModuleState CurrentState { get; set; } = ModuleState.NotLoaded;
            public string? LastError { get; set; }
        }

        public enum ModuleState
        {
            NotLoaded,
            Loading,
            Loaded,
            Unloading,
            Unloaded,
            Reloading,
            Error
        }

        /// <summary>
        /// Store or update module metadata
        /// </summary>
        public void StoreMetadata(string moduleName, ModuleMetadata metadata)
        {
            if (string.IsNullOrEmpty(moduleName))
                throw new ArgumentNullException(nameof(moduleName));

            _lock.EnterWriteLock();
            try
            {
                _cache[moduleName] = metadata ?? throw new ArgumentNullException(nameof(metadata));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get module metadata
        /// </summary>
        public ModuleMetadata? GetMetadata(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return null;

            _lock.EnterReadLock();
            try
            {
                return _cache.TryGetValue(moduleName, out var metadata) ? metadata : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Update module state
        /// </summary>
        public bool UpdateState(string moduleName, ModuleState newState, string? error = null)
        {
            if (string.IsNullOrEmpty(moduleName))
                return false;

            _lock.EnterWriteLock();
            try
            {
                if (_cache.TryGetValue(moduleName, out var metadata))
                {
                    metadata.CurrentState = newState;
                    metadata.LastError = error;

                    if (newState == ModuleState.Unloaded)
                        metadata.UnloadedAt = DateTime.UtcNow;

                    return true;
                }
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Check if module exists in cache
        /// </summary>
        public bool HasMetadata(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return false;

            _lock.EnterReadLock();
            try
            {
                return _cache.ContainsKey(moduleName);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove module metadata from cache
        /// </summary>
        public bool RemoveMetadata(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return false;

            _lock.EnterWriteLock();
            try
            {
                return _cache.TryRemove(moduleName, out _);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get all cached module names
        /// </summary>
        public IEnumerable<string> GetAllModuleNames()
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Keys.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all modules with a specific state
        /// </summary>
        public IEnumerable<ModuleMetadata> GetModulesByState(ModuleState state)
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Values.Where(m => m.CurrentState == state).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Clear all cached metadata
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }
    }
}