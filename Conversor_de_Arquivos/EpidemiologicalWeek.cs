namespace Conversor_de_Arquivos
{
    public static class EpidemiologicalWeek
    {
        public record WeekInfo(int Year, int Week, DateTime Start, DateTime End);

        private static DateTime StartOfWeek(DateTime date)
            => date.AddDays(-(int)date.DayOfWeek).Date;

        private static int YearOfWeek(DateTime start, DateTime end)
        {
            if (start.Year == end.Year) return start.Year;
            // Week spans Dec/Jan boundary — the year with >= 4 days owns the week
            int daysInStartYear = 31 - start.Day + 1;
            return daysInStartYear >= 4 ? start.Year : end.Year;
        }

        // Returns the Sunday that begins Week 1 of the given year.
        internal static DateTime FirstWeekStart(int year)
        {
            var jan1 = new DateTime(year, 1, 1);
            var candidate = StartOfWeek(jan1);
            var candidateEnd = candidate.AddDays(6);
            return YearOfWeek(candidate, candidateEnd) == year
                ? candidate
                : candidate.AddDays(7);
        }

        // Returns the start date (Sunday) of week `week` in year `year`.
        public static DateTime WeekStart(int year, int week)
            => FirstWeekStart(year).AddDays((week - 1) * 7);

        public static WeekInfo Calculate(DateTime date)
        {
            var start = StartOfWeek(date);
            var end = start.AddDays(6);
            int yearRef = YearOfWeek(start, end);
            var week1Start = FirstWeekStart(yearRef);
            int weekNum = (int)((start - week1Start).TotalDays / 7) + 1;

            if (weekNum <= 0)
            {
                yearRef--;
                week1Start = FirstWeekStart(yearRef);
                weekNum = (int)((start - week1Start).TotalDays / 7) + 1;
            }

            return new WeekInfo(yearRef, weekNum, start, end);
        }

        public static WeekInfo LastClosedWeek(DateTime today)
            => Calculate(StartOfWeek(today).AddDays(-1));
    }
}
