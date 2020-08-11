using System;
using System.Globalization;

namespace ProgramABB
{
    class Clock
    {
        public Clock()
        {
            SetSystemTime();
        }

        public void SetSystemTime()
        {
            time = DateTime.UtcNow;
        }

        public void AddSecond(int sec = 1)
        {
            time = time.AddSeconds(sec);
        }

        public void IncrementCounter()
        {
            Counter++;
        }

        public void ResetCounter()
        {
            Counter = 0;
        }

        public string Hour { get { return time.ToLocalTime().ToString("HH:mm"); } }
        public string DayOfWeek { get { return time.ToLocalTime().ToString("dddd", CultureInfo.CreateSpecificCulture("pl-PL")); } }
        public string Date { get { return time.ToLocalTime().ToString("dd/MM/yyyy"); } }

        public uint Counter { get; private set; } = 0;

        private DateTime time;
    }
}
