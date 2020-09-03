
using System;
using System.Diagnostics;

namespace CSharp_Library.Utility {

    public class TimeCounter {
        Stopwatch _watch = new Stopwatch();
        TimeSpan _lastCheck = new TimeSpan();

        public TimeCounter() {
            _watch.Start();
        }

        public TimeSpan ElapsedSinceLastCheck() {
            var elapsed = _watch.Elapsed;
            var elapsedTime = elapsed - _lastCheck;
            _lastCheck = elapsed;
            return elapsedTime;
        }

        public TimeSpan Elapsed() {
            return _watch.Elapsed;
        }

        public TimeSpan Restart() {
            var elapsed = _watch.Elapsed;
            _watch.Restart();
            return elapsed;
        }
    }

    public static class TimeCounterExt {
        public static double MillisecondsSinceLastCheck(this TimeCounter counter) {
            return counter.ElapsedSinceLastCheck().TotalMilliseconds;
        }
    }

}