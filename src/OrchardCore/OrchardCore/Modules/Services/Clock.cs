using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NodaTime.TimeZones;

namespace OrchardCore.Modules
{
    public class Clock : IClock
    {
        private static Instant CurrentInstant => SystemClock.Instance.GetCurrentInstant();

        /// <summary>
        /// Returns a Datetime Kind.Utc that is "Now"
        /// </summary>
        /// <inheritdoc />
        public DateTime UtcNow => CurrentInstant.ToDateTimeUtc();

        /// <summary>
        /// Returns a list of valid timezones as a ITimeZone[], where the key is
        /// the timezone id(string), and the value can be used for display. The list is filtered to contain only
        /// choices that are reasonably valid for the present and near future for real places. The list is
        /// also sorted first by UTC Offset and then by timezone name.
        /// </summary>
        public ITimeZone[] GetTimeZones()
        {
            var list =
                from location in TzdbDateTimeZoneSource.Default.ZoneLocations
                let zoneId = location.ZoneId
                let tz = DateTimeZoneProviders.Tzdb[zoneId]
                let zoneInterval = tz.GetZoneInterval(CurrentInstant)
                orderby zoneInterval.StandardOffset, zoneId
                select new TimeZone(zoneId, zoneInterval.StandardOffset, zoneInterval.WallOffset, tz);

            return list.ToArray();
        }

        public ITimeZone GetTimeZone(string timeZoneId)
        {
            if (String.IsNullOrEmpty(timeZoneId))
            {
                return GetSystemTimeZone();
            }

            var dateTimeZone = GetDateTimeZone(timeZoneId);

            return CreateTimeZone(dateTimeZone);
        }


        public ITimeZone GetSystemTimeZone()
        {
            var timezone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            if (TzdbDateTimeZoneSource.Default.CanonicalIdMap.ContainsKey(timezone.Id))
            {
                var canonicalTimeZoneId = TzdbDateTimeZoneSource.Default.CanonicalIdMap[timezone.Id];
                timezone = GetDateTimeZone(canonicalTimeZoneId);
            }
            return CreateTimeZone(timezone);
        }

        public DateTimeOffset ConvertToTimeZone(DateTimeOffset dateTimeOffSet, ITimeZone timeZone)
        {
            var offsetDateTime = OffsetDateTime.FromDateTimeOffset(dateTimeOffSet);
            return offsetDateTime.InZone(((TimeZone)timeZone).DateTimeZone).ToDateTimeOffset();
        }

        internal static DateTimeZone GetDateTimeZone(string timeZone)
        {
            if (!String.IsNullOrEmpty(timeZone) && IsValidTimeZone(DateTimeZoneProviders.Tzdb, timeZone))
            {
                return DateTimeZoneProviders.Tzdb[timeZone];
            }

            return DateTimeZoneProviders.Tzdb.GetSystemDefault();
        }

        private ITimeZone CreateTimeZone(DateTimeZone dateTimeZone)
        {
            if (dateTimeZone == null)
            {
                throw new ArgumentException(nameof(DateTimeZone));
            }

            var zoneInterval = dateTimeZone.GetZoneInterval(CurrentInstant);

            return new TimeZone(dateTimeZone.Id, zoneInterval.StandardOffset, zoneInterval.WallOffset, dateTimeZone);
        }

        private static bool IsValidTimeZone(IDateTimeZoneProvider provider, string timeZoneId)
        {
            return provider.GetZoneOrNull(timeZoneId) != null;
        }
    }
}
