using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace OrchardCore.Localization
{
    public class DefaultCalendarManager : ICalendarManager
    {
        private readonly IEnumerable<ICalendarSelector> _calendarSelectors;

        public DefaultCalendarManager(IEnumerable<ICalendarSelector> calendarSelectors)
        {
            _calendarSelectors = calendarSelectors;
        }

        public async Task<CalendarSystem> GetCurrentCalendar()
        {
            var calendarName = await GetCalendarAsync();

            return BclCalendars.GetCalendarByName(calendarName);
        }

        private async Task<CalendarName> GetCalendarAsync()
        {
            var calendarResults = new List<CalendarSelectorResult>();

            foreach (var calendarSelector in _calendarSelectors)
            {
                var calendarResult = await calendarSelector.GetCalendarAsync();

                if (calendarResult != null)
                {
                    calendarResults.Add(calendarResult);
                }
            }

            if (calendarResults.Count == 0)
            {
                return CalendarName.Unknown;
            }
            else if (calendarResults.Count > 1)
            {
                calendarResults.Sort((x, y) => y.Priority.CompareTo(x.Priority));
            }

            var calendarName = await calendarResults.First().CalendarName();

            return calendarName;
        }
    }
}