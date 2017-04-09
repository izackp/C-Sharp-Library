using System.Collections.Generic;
using System;
using System.Threading;

namespace CSharp_Library.Utility {
    //Avoids creating more threads by using the global queue
    //Otherwise you could just create an instance of DispatchConcurrentQueue with just 1 worker for a similar effect
    public class DispatchQueueSerial : Dispatch {
        DispatchConcurrentQueue _dispatchQueue;
        Queue<Action> _actionQueue = new Queue<Action>();
        bool _finished = true;
        Priority _priority;

        public DispatchQueueSerial(string name, Priority priority = Priority.Normal) : base(name) {
            _priority = priority;
        }

        void Run() {
            Action act;
            lock (_actionQueue) {
                if (_actionQueue.Count == 0) {
                    _finished = true;
                    return;
                }
                act = _actionQueue.Dequeue();
            }

            act.Invoke();
            Dispatch.AsyncGlobal(Run, _priority);
        }

        public void Async(Action act) {
            lock (_actionQueue) {
                _actionQueue.Enqueue(act);
                if (_finished) {
                    _finished = false;
                    Dispatch.AsyncGlobal(Run, _priority);
                }
            }
        }

        void Sync(Action act) {
            ManualResetEvent wait = new ManualResetEvent(false);
            Async(() => {
                act.Invoke();
                wait.Set();
            });
            wait.WaitOne();
        }
    }
}