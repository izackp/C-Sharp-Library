using System;
using System.Collections.Generic;
using System.Threading;

/*
    Inspired by LibDispatch

    Things I may add:
    Set.Get Context
    dispatch_after
    Pipe/Channel (pass array of actions)
*/

namespace CSharp_Library.Utility {
    public abstract class Dispatch {
        protected string _name;

        public Dispatch(string name) {
            _name = name;
        }

        public string Name() {
            return _name;
        }

        public enum Priority {
            Lowest,
            Low,
            Normal,
            High,
            Highest
        }

        //Conviencance Functions
        public static void AsyncGlobal(Action task, Priority priority = Priority.Normal) {
            DispatchConcurrentQueue.GlobalQueue().Async(task, priority);
        }

        public static void SyncGlobal(Action task, Priority priority = Priority.Normal) {
            DispatchConcurrentQueue.GlobalQueue().Sync(task, priority);
        }
    }

    public class DispatchConcurrentQueue : Dispatch, IDisposable {

        readonly LinkedList<Thread> _workers;
        readonly Queue<Action>      _tasks_highest  = new Queue<Action>();
        readonly Queue<Action>      _tasks_high     = new Queue<Action>();
        readonly Queue<Action>      _tasks          = new Queue<Action>();
        readonly Queue<Action>      _tasks_low      = new Queue<Action>();
        readonly Queue<Action>      _tasks_lowest   = new Queue<Action>();
        bool                        _disallowAdd;
        bool                        _disposed;
        bool                        _suspended      = false;

        public bool KeepAlive = false; //Will block the disposing thread until all tasks are finished

        public DispatchConcurrentQueue(string name, int size) : base(name) {
            _workers = new LinkedList<Thread>();
            for (var i = 0; i < size; ++i) {
                var worker = new Thread(Worker) { Name = string.Concat(_name, " - Worker ", i) };
                worker.Start();
                _workers.AddLast(worker);
            }
        }

        static DispatchConcurrentQueue sGlobalQueue;
        public static DispatchConcurrentQueue GlobalQueue() {
            if (sGlobalQueue == null)
                sGlobalQueue = new DispatchConcurrentQueue("Global Queue", 4);
            return sGlobalQueue;
        }

        public void Dispose() {
            if (_disposed)
                return;

            lock (_tasks) {
                GC.SuppressFinalize(this);

                _disallowAdd = true;
                if (KeepAlive) {
                    if (_suspended) {
                        Console.WriteLine("Warning: Cannot keep alive while queue is suspended.");
                    } else {
                        while (TotalTaskCount() > 0) {
                            Monitor.Wait(_tasks);
                        }
                    }
                }

                _disposed = true;
                Monitor.PulseAll(_tasks); // wake all workers (disposed flag will cause then to finish so that we can join them)
            }

            foreach (var worker in _workers) {
                worker.Join();
            }
        }

        public void Async(Action task, Priority priority = Priority.Normal) {
            lock (_tasks) {
                if (_disallowAdd) { throw new InvalidOperationException("This Pool instance is in the process of being disposed, can't add anymore"); }
                if (_disposed) { throw new ObjectDisposedException("This Pool instance has already been disposed"); }

                Queue<Action> queue = QueueForPriority(priority);
                queue.Enqueue(task);
                Monitor.Pulse(_tasks);
            }
        }

        public void Sync(Action act, Priority priority = Priority.Normal) {
            if (IsSuspended())
                Console.WriteLine("Warning: Calling a synchronous method on a suspended queue which will not execute until resumed.");

            ManualResetEvent wait = new ManualResetEvent(false);
            Async(() => {
                act.Invoke();
                wait.Set();
            }, priority);
            wait.WaitOne();
        }

        private Queue<Action> QueueForPriority(Priority priority) {
            switch (priority) {
                case Priority.Lowest:
                    return _tasks_lowest;

                case Priority.Low:
                    return _tasks_low;

                case Priority.Normal:
                    return _tasks;

                case Priority.High:
                    return _tasks_high;

                case Priority.Highest:
                    return _tasks_highest;
            }
            Console.WriteLine("Warning: Invalid argument provided to QueueForPriority - " + (int)priority);
            return _tasks;
        }

        public void Suspend() {
            _suspended = true;
        }

        public void Resume() {
            if (_suspended == false)
                return;
            _suspended = false;
            lock (_tasks) {
                Monitor.PulseAll(_tasks);
            }
        }

        public bool IsSuspended() {
            return _suspended;
        }

        private void Worker() {
            Action task = null;
            while (true) {
                task = GetTaskOrWait();
                if (task == null)
                    return;
                task();
                task = null;
            }
        }

        private Action GetTaskOrWait() {
            lock (_tasks) {
                while (true) {
                    if (_disposed)
                        return null;

                    if (_suspended == false) {
                        Action task = NextPrioritizedTask();
                        if (task != null) {
                            Monitor.PulseAll(_tasks);
                            return task;
                        }
                    }
                    Monitor.Wait(_tasks);
                }
            }
        }

        private Action NextPrioritizedTask() {
            if (_tasks_highest.Count > 0)
                return _tasks_highest.Dequeue();

            if (_tasks_high.Count > 0)
                return _tasks_high.Dequeue();

            if (_tasks.Count > 0)
                return _tasks.Dequeue();

            if (_tasks_low.Count > 0)
                return _tasks_low.Dequeue();

            if (_tasks_lowest.Count > 0)
                return _tasks_lowest.Dequeue();

            return null;
        }

        private int TotalTaskCount() {
            return _tasks_highest.Count + _tasks_high.Count + _tasks.Count + _tasks_low.Count + _tasks_lowest.Count;
        }
    }

    public class QueueTest {

        public static void Run() {
            DispatchConcurrentQueue dcq = DispatchConcurrentQueue.GlobalQueue();
            dcq.Suspend();

            dcq.Async(() => {
                Console.WriteLine("Starting Task High");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task High");
            }, Dispatch.Priority.High);

            dcq.Async(() => {
                Console.WriteLine("Starting Task Lowest");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task Lowest");
            }, Dispatch.Priority.Lowest);

            dcq.Async(() => {
                Console.WriteLine("Starting Task 1");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task 1");
            });

            dcq.Async(() => {
                Console.WriteLine("Starting Task 2");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task 2");
            });

            dcq.Async(() => {
                Console.WriteLine("Starting Task 3");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task 3");
            });

            dcq.Async(() => {
                Console.WriteLine("Starting Task 4");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task 4");
            });

            dcq.Async(() => {
                Console.WriteLine("Starting Task 5");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task 5");
            });

            dcq.Async(() => {
                Console.WriteLine("Starting Task Highest");
                Thread.Sleep(1000);
                Console.WriteLine("Finish Task Highest");
            }, Dispatch.Priority.Highest);

            dcq.Resume();

            Console.WriteLine("Waiting on Sync");
            dcq.Sync(() => {
                Console.WriteLine("Sync");
            }, Dispatch.Priority.Lowest);
            Thread.Sleep(100000000);
        }
    }
}