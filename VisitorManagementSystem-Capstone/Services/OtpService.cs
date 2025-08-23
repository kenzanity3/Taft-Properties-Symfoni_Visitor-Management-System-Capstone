using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using VisitorManagementSystem_Capstone.Models;

namespace VisitorManagementSystem_Capstone.Services
{
    /// <summary>
    /// Handles the generation, storage, validation, and management of OTP codes using in-memory caching.
    /// </summary>
    public static class OtpService
    {
        // Memory cache instance to store OTP entries temporarily
        private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        // Stores active OTP entries directly (key = OTP code, value = OtpEntry object)
        private static readonly ConcurrentDictionary<string, OtpEntry> _activeOtpCodes = new();

        /// <summary>
        /// Generates a random 6-character OTP for a room owner, with optional usage limits.
        /// The OTP automatically expires after 25 minutes.
        /// </summary>
        /// <param name="RoomOwnerId">The room owner identifier associated with the OTP.</param>
        /// <param name="maxUsage">Optional maximum number of times the OTP can be used (1–5). Defaults to 1 if not provided.</param>
        /// <returns>The generated 6-character OTP code.</returns>
        public static string GenerateRoomOwnerOtp(string RoomOwnerId, int? maxUsage)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            // 🔍 Remove any existing OTP associated with the same RoomOwnerId
            foreach (var otpCode in _activeOtpCodes.Keys)
            {
                if (_cache.TryGetValue<OtpEntry>(otpCode, out var existingEntry))
                {
                    if (existingEntry.Id == RoomOwnerId)
                    {
                        _cache.Remove(otpCode);
                        _activeOtpCodes.TryRemove(otpCode, out _);
                        break; // Assumes one active OTP per room owner
                    }
                }
            }

            // 🔐 Generate new OTP
            string otp = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var entry = new OtpEntry
            {
                Code = otp,
                Id = RoomOwnerId,
                ExpirationTime = DateTime.UtcNow.AddMinutes(25),
                MaxUsage = (maxUsage.HasValue && maxUsage.Value > 0 && maxUsage.Value <= 5)
                          ? maxUsage.Value
                          : 1
            };


            // ⏳ Set expiration and register eviction callback
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(25)
            };
            // When the cache entry expires, also remove it from our dictionary
            cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (key is string otpKey)
                {
                    _activeOtpCodes.TryRemove(otpKey, out _);
                }
            });

            // 💾 Store in cache and tracking dictionary
            _cache.Set(otp, entry, cacheEntryOptions);
            _activeOtpCodes.TryAdd(otp, entry);

            return otp;
        }


        /// <summary>
        /// Validates a room owner OTP against the cache and user usage.
        /// Increments usage count and removes OTP after max usage is reached.
        /// </summary>
        /// <param name="otpInput">The OTP code to validate.</param>
        /// <param name="userid">The user ID attempting to use the OTP.</param>
        /// <returns><c>true</c> if OTP is valid and user hasn't used it yet; otherwise, <c>false</c>.</returns>
        public static bool ValidateRoomOwnerOtp(string otpInput, string userid)
        {
            if (_activeOtpCodes.TryGetValue(otpInput, out var entry))
            {
                // Check if OTP is expired
                if (entry.ExpirationTime < DateTime.UtcNow)
                {
                    _cache.Remove(otpInput);
                    _activeOtpCodes.TryRemove(otpInput, out _);
                    return false;
                }

                // User hasn't used this OTP yet
                if (!entry.userids.Contains(userid))
                {
                    entry.userids.Add(userid);

                    // If usage limit reached, remove OTP
                    if (entry.userids.Count >= entry.MaxUsage)
                    {
                        _cache.Remove(otpInput);
                        _activeOtpCodes.TryRemove(otpInput, out _);
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the remaining time before an OTP expires.
        /// </summary>
        /// <param name="otpInput">The OTP code to check.</param>
        /// <returns>The remaining <see cref="TimeSpan"/> if OTP exists and is valid; otherwise, <c>null</c>.</returns>
        public static TimeSpan? GetOtpRemainingTime(string otpInput)
        {
            if (_activeOtpCodes.TryGetValue(otpInput, out var entry))
            {
                var remaining = entry.ExpirationTime - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                    return remaining;
            }
            return null;
        }

        /// <summary>
        /// Removes the specified OTP code from the cache and the active OTP dictionary, effectively invalidating it.
        /// </summary>
        /// <param name="otpCode">The OTP to remove.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public static Task ForceRemoveOTP(string otpCode)
        {
            // Remove the OTP from both cache and dictionary
            _cache.Remove(otpCode);
            _activeOtpCodes.TryRemove(otpCode, out _);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Generates a random 6-character OTP for facility use.
        /// The OTP automatically expires after 1 hour.
        /// </summary>
        /// <param name="FacilityLogId">The facility log identifier associated with the OTP.</param>
        /// <param name="userid">The single user ID authorized to use this OTP.</param>
        /// <returns>The generated 6-character OTP code.</returns>
        public static string GenerateFacilityOtp(string FacilityLogId, string userid)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string otp = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var entry = new OtpEntry
            {
                Code = otp,
                Id = FacilityLogId,
                userids = new List<string> { userid },
                ExpirationTime = DateTime.UtcNow.AddHours(1),
                MaxUsage = 1
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };

            cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (key is string otpKey)
                {
                    _activeOtpCodes.TryRemove(otpKey, out _);
                }
            });

            _cache.Set(otp, entry, cacheEntryOptions);
            _activeOtpCodes.TryAdd(otp, entry);

            return otp;
        }

        /// <summary>
        /// Validates a facility OTP by verifying if the user ID is authorized.
        /// </summary>
        /// <param name="otpInput">The OTP code to validate.</param>
        /// <param name="userid">The user ID attempting to use the OTP.</param>
        /// <returns><c>true</c> if the OTP is valid for the user; otherwise, <c>false</c>.</returns>
        public static bool ValidateFacilityOtp(string otpInput, string userid)
        {
            if (_activeOtpCodes.TryGetValue(otpInput, out var entry))
            {
                return entry.userids.Contains(userid);
            }
            return false;
        }

        /// <summary>
        /// Extends the expiration of a facility OTP if the user is authorized.
        /// </summary>
        /// <param name="otpInput">The OTP code to extend.</param>
        /// <param name="userid">The user ID attempting to extend the OTP.</param>
        /// <returns><c>true</c> if the OTP was extended successfully; otherwise, <c>false</c>.</returns>
        public static bool ExtendFacilityOtp(string otpInput, string userid)
        {
            if (_activeOtpCodes.TryGetValue(otpInput, out var entry))
            {
                if (entry.userids.Contains(userid))
                {
                    entry.ExpirationTime = DateTime.UtcNow.AddHours(1);

                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    };

                    cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
                    {
                        if (key is string otpKey)
                        {
                            _activeOtpCodes.TryRemove(otpKey, out _);
                        }
                    });

                    _cache.Set(otpInput, entry, cacheEntryOptions);
                    _activeOtpCodes[otpInput] = entry; // Update dictionary entry

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves an OTP entry from the cache by OTP code.
        /// </summary>
        /// <param name="otpInput">The OTP code to look up.</param>
        /// <returns>The corresponding <see cref="OtpEntry"/> if found; otherwise, <c>null</c>.</returns>
        public static OtpEntry? GetOtpEntry(string otpInput)
        {
            _activeOtpCodes.TryGetValue(otpInput, out var entry);
            return entry;
        }
        /// <summary>
        /// Retrieves all active OTP entries currently in memory.
        /// </summary>
        /// <returns>A collection of active <see cref="OtpEntry"/> objects.</returns>
        public static IEnumerable<OtpEntry> GetAllActiveOtps()
        {
            return _activeOtpCodes.Values;
        }
    }
}
