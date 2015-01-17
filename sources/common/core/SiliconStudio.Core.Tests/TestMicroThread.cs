﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using SiliconStudio.Core.MicroThreading;

#if NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace SiliconStudio.Core.Tests
{
    // TODO: Add some checks to see if tests really complete within scheduler.Step() callstack
    // (if something is wrong with scheduling, it could end up being ran on another thread).
    [TestFixture]
    [Description("Tests on Scheduler and MicroThread")]
    public class TestMicroThread
    {
        public class BaseTests
        {
            public int SharedCounter;
            public int completed;
        }

        // We want to generate async methods for test purpose
#pragma warning disable 1998
        
        public class SimpleTests : BaseTests
        {
            protected async Task TestSpecialHelper()
            {
                await TaskEx.Delay(100);
            }

            public async Task TestSpecial(Action completed)
            {
                int counter = SharedCounter;
                await TestSpecialHelper();
                Assert.That(SharedCounter, Is.Not.EqualTo(counter));
                completed();
            }

            public async Task TestAwaitDelayAsync(Action completed)
            {
                int counter = SharedCounter;
                await TaskEx.Delay(100);
                Assert.That(SharedCounter, Is.Not.EqualTo(counter));
                completed();
            }

            public async Task TestWaitDelayAsync(Action completed)
            {
                int counter = SharedCounter;
                TaskEx.Delay(100).Wait();
                Assert.That(SharedCounter, Is.EqualTo(counter));
                completed();
            }

            //public async Task TestAwaitDownloadAsync(Action completed)
            //{
            //    int counter = SharedCounter;
            //    var wc = new WebClient();
            //    await wc.DownloadDataTaskAsync(new Uri("http://www.google.com"));
            //    Assert.That(SharedCounter, Is.Not.EqualTo(counter));
            //    completed();
            //}
            //
            //public async Task TestWaitDownloadAsync(Action completed)
            //{
            //    int counter = SharedCounter;
            //    var wc = new WebClient();
            //    wc.DownloadDataTaskAsync(new Uri("http://www.google.com")).Wait();
            //    Assert.That(SharedCounter, Is.EqualTo(counter));
            //    completed();
            //}

            protected async Task TestInsideWaitHelperAsync2()
            {
                TaskEx.Delay(200).Wait();
                await TaskEx.Delay(200);
            }

            protected async Task TestInsideWaitHelperAsync()
            {
                await TaskEx.Delay(200);
                TestInsideWaitHelperAsync2().Wait();
            }

            //public async Task TestAwaitInsideWaitAsync(Action completed)
            //{
            //    int counter = SharedCounter;
            //    await TestInsideWaitHelperAsync();
            //    Assert.That(SharedCounter, Is.Not.EqualTo(counter));
            //    completed();
            //}

            public async Task TestAwaitThreadingTaskAsync(Action completed)
            {
                int counter = SharedCounter;
                await Task.Factory.StartNew(() => Thread.Sleep(1000));
                Assert.That(SharedCounter, Is.Not.EqualTo(counter));
                completed();
            }

            public async Task TestWaitThreadingTaskAsync(Action completed)
            {
                int counter = SharedCounter;
                Task.Factory.StartNew(() => Thread.Sleep(1000)).Wait();
                Assert.That(SharedCounter, Is.EqualTo(counter));
                completed();
            }

            protected async Task TestAwaitDirectAsyncHelper()
            {
                var tcs = new TaskCompletionSource<int>();
                tcs.TrySetResult(3);
                await tcs.Task;
                await tcs.Task;
            }

            public async Task TestAwaitDirectAsync(Action completed)
            {
                int counter = SharedCounter;
                await TestAwaitDirectAsyncHelper();
                Assert.That(SharedCounter, Is.EqualTo(counter));
                completed();
            }

            public async Task TestWaitFrameAsync(Action completed)
            {
                int counter = SharedCounter;
                for (int i = 0; i < 8; ++i)
                    await Scheduler.Current.NextFrame();
                Assert.That(SharedCounter, Is.EqualTo(counter + 8));
                completed();
            }

            protected Task TestWaitMultipleAsyncHelper2()
            {
                return Task.Factory.StartNew(() => Thread.SpinWait(100000));
            }

            protected async Task TestWaitMultipleAsyncHelper()
            {
                await TaskEx.WhenAll(TaskEx.Delay(1000), TestWaitMultipleAsyncHelper2());
            }

            public async Task TestWaitMultipleAsync(Action completed)
            {
                await TestWaitMultipleAsyncHelper();
                completed();
            }

            protected async Task TestWaitForkingAsyncHelper()
            {
                await TaskEx.Delay(10);
            }

            public async Task TestWaitForkingAsync(Action completed)
            {
                await TaskEx.WhenAll(TestWaitForkingAsyncHelper(), TestWaitForkingAsyncHelper());
                completed();
            }

            protected async Task TestThrowAsyncHelper()
            {
                throw new InvalidOperationException();
            }

            public async Task TestThrowAsync(Action completed)
            {
                try
                {
                    await TestThrowAsyncHelper();
                }
                catch {}
                completed();
            }

            public async Task TestThrowExternalAsync(Action completed)
            {
                try
                {
                    await TestThrowAsyncHelper();
                }
                catch { }
                completed();
            }
        }

        public class ThrowTests : BaseTests
        {
            protected async Task TestThrowAsyncHelper()
            {
                throw new InvalidOperationException();
            }

            public async Task TestThrowAfterAsync(Action completed)
            {
                await TaskEx.Delay(100);
                completed();
                await TestThrowAsyncHelper();
            }

            public async Task TestThrowAsync(Action completed)
            {
                completed();
                await TestThrowAsyncHelper();
            }
        }

        public class SyncTests : BaseTests
        {
            protected async Task TestSleep()
            {
                await TaskEx.Delay(200);
            }

            public async Task TestWaitMicroThread(Action completed)
            {
                int counter = SharedCounter;
                await Scheduler.Current.WhenAll(Scheduler.Current.Add(TestSleep), Scheduler.Current.Add(TestSleep));
                Assert.That(SharedCounter, Is.Not.EqualTo(counter));
                completed();
            }
        }

#pragma warning restore 1998
        
        public object[] GenerateFunctions<T>() where T : new()
        {
            var result = new List<object[]>();
            foreach (var method in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var obj = new T();
                result.Add(new object[] { method.Name, obj, new Func<Action, Task>((completedAction) => (Task)method.Invoke(obj, new object[] { completedAction })), 2 });
            }
            return result.ToArray();
        }

        public object[] Functions
        {
            get { return GenerateFunctions<SimpleTests>(); }
        }

        public object[] FunctionsThrow
        {
            get { return GenerateFunctions<ThrowTests>(); }
        }

        public object[] FunctionsSync
        {
            get { return GenerateFunctions<SyncTests>(); }
        }

        void CheckStackForSchedulerStep()
        {
            foreach (StackFrame stackFrame in new StackTrace().GetFrames())
            {
                if (stackFrame.GetMethod().DeclaringType == typeof(Scheduler) && stackFrame.GetMethod().Name == "Run")
                {
                    return;
                }
            }
            throw new InvalidOperationException("Callstack at end of MicroThread should contain Scheduler.Step().");
        }

        protected MicroThread[] TestBase(string testName, BaseTests baseTests, Func<Action, Task> asyncFunction, int parallelCount, MicroThreadFlags flags = MicroThreadFlags.None)
        {
            var scheduler = new Scheduler();
            int completed = 0;
            var microThreads = new MicroThread[parallelCount];

            // Run two microthreads at the same time
            for (int i = 0; i < parallelCount; ++i)
                microThreads[i] = scheduler.Add(() => asyncFunction(() => { Interlocked.Increment(ref completed); CheckStackForSchedulerStep(); }), flags);

            // Simulation of main loop
            for (int i = 0; i < 1000 && scheduler.MicroThreads.Count() > 0; ++i)
            {
                baseTests.SharedCounter = i;
                scheduler.Run();
                Thread.Sleep(10);
            }

            // Check both microthreads completed
            Assert.That(completed, Is.EqualTo(parallelCount));

            return microThreads;
        }

        [Test, Sequential, TestCaseSource("Functions")]
        public void TestFunctions(string testName, BaseTests baseTests, Func<Action, Task> asyncFunction, int parallelCount)
        {
            var microThreads = TestBase(testName, baseTests, asyncFunction, parallelCount);
            Assert.That(microThreads.All(x => x.State == MicroThreadState.Completed), Is.EqualTo(true));
        }
        
        [Test, Sequential, TestCaseSource("FunctionsThrow")]
        public void TestExceptionsIgnore(string testName, BaseTests baseTests, Func<Action, Task> asyncFunction, int parallelCount)
        {
            var microThreads = TestBase(testName, baseTests, asyncFunction, parallelCount, MicroThreadFlags.IgnoreExceptions);
            Assert.That(microThreads.All(x => x.State == MicroThreadState.Failed && x.Exception != null), Is.EqualTo(true));
        }

        [Test, ExpectedException(typeof(InvalidOperationException)), Sequential, TestCaseSource("FunctionsThrow")]
        public void TestExceptions(string testName, BaseTests baseTests, Func<Action, Task> asyncFunction, int parallelCount)
        {
            var microThreads = TestBase(testName, baseTests, asyncFunction, parallelCount);
            Assert.That(microThreads.All(x => x.State == MicroThreadState.Failed && x.Exception != null), Is.EqualTo(true));
        }

        [Test, Sequential, TestCaseSource("FunctionsSync")]
        public void TestSyncs(string testName, BaseTests baseTests, Func<Action, Task> asyncFunction, int parallelCount)
        {
            var microThreads = TestBase(testName, baseTests, asyncFunction, parallelCount);
            Assert.That(microThreads.All(x => x.State == MicroThreadState.Completed), Is.EqualTo(true));
        }

        /*[Test]
        public void TestSwitchToNewMicrothread()
        {
            int completed = 0;

            var scheduler = new Scheduler();
            Action test = async () =>
                {
                    using (await scheduler.SwitchToNewMicroThread())
                    {
                        await TaskEx.Delay(100);
                        await scheduler.WaitFrame();
                        await TaskEx.Delay(100);
                        await scheduler.WaitFrame();
                        Interlocked.Increment(ref completed);
                    }
                };
            test();

            // Simulation of main loop
            for (int i = 0; i < 1000 && scheduler.MicroThreads.Count() > 0; ++i)
            {
                scheduler.Step();
                Thread.Sleep(10);
            }

            Assert.AreEqual(null, SynchronizationContext.Current);
            Assert.AreEqual(0, scheduler.MicroThreads.Count());
            Assert.AreEqual(1, completed);
        }*/
        
        public async Task TestTaskCompletionSourceAsync(TaskCompletionSource<int> tcs, Action completed)
        {
            await tcs.Task;
            completed();
        }

        [Test]
        public void TestTaskCompletionSource()
        {
            var scheduler = new Scheduler();
            var tcs = new TaskCompletionSource<int>();
            bool completed = false;
            scheduler.Add(() => TestTaskCompletionSourceAsync(tcs, () => { completed = true; CheckStackForSchedulerStep(); }));

            // Simulation of main loop
            for (int i = 0; i < 10 && scheduler.MicroThreads.Count() > 0; ++i)
            {
                scheduler.Run();
                tcs.TrySetResult(1);
                Thread.Sleep(10);
            }
            Assert.That(completed, Is.EqualTo(true));
        }
    }
}
