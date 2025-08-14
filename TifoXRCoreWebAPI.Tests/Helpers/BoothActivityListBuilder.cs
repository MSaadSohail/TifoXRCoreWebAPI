// <copyright file="BoothActivityListBuilder.cs" company="Global Mobile Software">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/13/2025</date>
// <summary>Fluent builder for a list of BoothActivity test records.</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds a List for BoothActivity with sensible defaults and chainable overrides,
    /// </summary>
    public class BoothActivityListBuilder
    {
        private readonly List<BoothActivity> _items = new();

        /// <summary>
        /// Start with n activities with predictable, valid defaults.
        /// </summary>
        public BoothActivityListBuilder WithCount(int n)
        {
            _items.Clear();
            var now = DateTime.UtcNow;

            for (int i = 0; i < n; i++)
            {
                _items.Add(new BoothActivity
                {
                    BoothId = 100 + i,
                    UserId = $"user-{i}",
                    ExitCode = 0,
                    BoothNameKey = $"booth_key_{i}",
                    SessionId = Guid.NewGuid().ToString("N"),
                    EntryDatetime = now.AddMinutes(-10 - i),
                    SessionDuration = 600 + i, // seconds
                    ExitDatetime = now.AddMinutes(-i)
                });
            }
            return this;
        }

        /// <summary>
        /// Set a specific ExitDatetime on the item at index i.
        /// </summary>
        public BoothActivityListBuilder WithExitDatetime(int index, DateTime? exit)
        {
            if (index >= 0 && index < _items.Count)
            {
                _items[index].ExitDatetime = exit;
            }
            return this;
        }

        /// <summary>
        /// Finalize the list.
        /// </summary>
        public List<BoothActivity> Build()
        {
            // Return a new list so tests don’t accidentally mutate internal state.
            return _items.Select(x => new BoothActivity
            {
                BoothId = x.BoothId,
                UserId = x.UserId,
                ExitCode = x.ExitCode,
                BoothNameKey = x.BoothNameKey,
                SessionId = x.SessionId,
                EntryDatetime = x.EntryDatetime,
                SessionDuration = x.SessionDuration,
                ExitDatetime = x.ExitDatetime
            }).ToList();
        }
    }
}
