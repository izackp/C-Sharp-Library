using System.Collections.Generic;
using System;
using System.Threading;

namespace CSharp_Library.Utility {
    //Avoids creating more threads by using the global queue
    //Otherwise you could just create an instance of DispatchConcurrentQueue with just 1 worker for a similar effect
    //The serial nature comes from the fact that this class adds only 1 job at a time to the concurrent queue.
    //Once one is finished the next is added so its not possible for more than 1 job to execute simulatiously.
    public class DispatchSerialQueue : Dispatch {
        readonly Queue<Action> _actionQueue = new Queue<Action>();
        readonly Priority _priority;
        bool _finished = true;

        public DispatchSerialQueue(string name, Priority priority = Priority.Normal) : base(name) {
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

        public void Sync(Action act) {
            ManualResetEvent wait = new ManualResetEvent(false);
            Async(() => {
                act.Invoke();
                wait.Set();
            });
            wait.WaitOne();
        }
    }
}