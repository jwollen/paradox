﻿using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using NUnit.Framework;

using SiliconStudio.Presentation.Services;
using SiliconStudio.Presentation.View;

namespace SiliconStudio.Presentation.Tests
{
    [TestFixture]
    class TestDispatcher
    {
        [Test]
        public void TestInvoke()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            dispatcher.Invoke(() => count = 2);
            Assert.AreEqual(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Test]
        public void TestInvokeResult()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            int result = dispatcher.Invoke(() => ++count);
            Assert.AreEqual(2, result);
            ShutdownDispatcher(dispatcher);
        }

        [Test]
        public void TestInvokeAsyncFireAndForget()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            dispatcher.BeginInvoke(async () => { await Task.Delay(100); count = count + 1; });
            Assert.AreEqual(1, count);
            Thread.Sleep(200);
            Assert.AreEqual(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Test]
        public void TestInvokeTask()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); count = count + 1; });
            Assert.AreEqual(1, count);
            task.Result.Wait();
            Assert.AreEqual(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Test]
        public void TestInvokeTaskResult()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); return 2; });
            Assert.AreEqual(1, count);
            task.Wait();
            count += task.Result.Result;
            Assert.AreEqual(3, count);
            ShutdownDispatcher(dispatcher);
        }
        
        [Test]
        public async void TestInvokeAsyncTask()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); count = count + 1; });
            Assert.AreEqual(1, count);
            await task.Result;
            Assert.AreEqual(2, count);
            ShutdownDispatcher(dispatcher);
        }

        [Test]
        public async void TestInvokeAsyncTaskResult()
        {
            var dispatcher = CreateDispatcher();
            int count = 1;
            var task = dispatcher.InvokeAsync(async () => { await Task.Delay(100); return 2; });
            Assert.AreEqual(1, count);
            count += await task.Result;
            Assert.AreEqual(3, count);
            ShutdownDispatcher(dispatcher);
        }

        static void ShutdownDispatcher(IDispatcherService dispatcher)
        {
            dispatcher.Invoke(() => Dispatcher.CurrentDispatcher.InvokeShutdown());
        }

        static IDispatcherService CreateDispatcher()
        {
            var initializationSignal = new AutoResetEvent(false);
            IDispatcherService result = null;
            var dispatcherThread = new Thread(() =>
            {
                result = DispatcherService.Create();
                initializationSignal.Set();
                Dispatcher.Run();
            });
            dispatcherThread.Start();
            initializationSignal.WaitOne();
            return result;
        }
    }
}
