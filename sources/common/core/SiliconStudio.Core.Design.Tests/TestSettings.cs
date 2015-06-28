﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;
using SiliconStudio.Core.Settings;

namespace SiliconStudio.Core.Design.Tests
{
    class ValueSettingsKeys
    {
        public static SettingsValueKey<int> IntValue;
        public static SettingsValueKey<double> DoubleValue;
        public static SettingsValueKey<string> StringValue;

        public static void Initialize()
        {
            IntValue = new SettingsValueKey<int>("Test/Simple/IntValue", TestSettings.SettingsGroup, 10);
            DoubleValue = new SettingsValueKey<double>("Test/Simple/DoubleValue", TestSettings.SettingsGroup, 3.14);
            StringValue = new SettingsValueKey<string>("Test/Simple/StringValue", TestSettings.SettingsGroup, "Test string");
            Console.WriteLine(@"Static settings keys initialized (ValueSettingsKeys)");
        }
    }

    class ListSettingsKeys
    {
        public static SettingsValueKey<List<int>> IntList;
        public static SettingsValueKey<List<double>> DoubleList;
        public static SettingsValueKey<List<string>> StringList;

        public static void Initialize()
        {
            IntList = new SettingsValueKey<List<int>>("Test/Lists/IntList", TestSettings.SettingsGroup, Enumerable.Empty<int>().ToList());
            DoubleList = new SettingsValueKey<List<double>>("Test/Lists/DoubleList", TestSettings.SettingsGroup, new[] { 2.0, 6.0, 9.0 }.ToList());
            StringList = new SettingsValueKey<List<string>>("Test/Lists/StringList", TestSettings.SettingsGroup, new[] { "String 1", "String 2", "String 3" }.ToList());
            Console.WriteLine(@"Static settings keys initialized (ListSettingsKeys)");
        }
    }

    [TestFixture]
    class TestSettings
    {
        public static Guid SessionGuid = Guid.NewGuid();
        public static SettingsGroup SettingsGroup = new SettingsGroup();
        [SetUp]
        public static void InitializeSettings()
        {
            SettingsGroup.ClearSettings();
        }

        public static string TempPath(string file)
        {
            var dir = Path.Combine(Path.GetTempPath(), SessionGuid.ToString());
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, file);
        }

        [Test]
        public void TestSettingsInitialization()
        {
            ValueSettingsKeys.Initialize();
            Assert.AreEqual(10, ValueSettingsKeys.IntValue.GetValue());
            Assert.AreEqual(3.14, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.AreEqual("Test string", ValueSettingsKeys.StringValue.GetValue());
        }

        [Test]
        public void TestSettingsWrite()
        {
            ValueSettingsKeys.Initialize();
            ValueSettingsKeys.IntValue.SetValue(20);
            ValueSettingsKeys.DoubleValue.SetValue(6.5);
            ValueSettingsKeys.StringValue.SetValue("New string");
            Assert.AreEqual(20, ValueSettingsKeys.IntValue.GetValue());
            Assert.AreEqual(6.5, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.AreEqual("New string", ValueSettingsKeys.StringValue.GetValue());

            ValueSettingsKeys.IntValue.SetValue(30);
            ValueSettingsKeys.DoubleValue.SetValue(9.1);
            ValueSettingsKeys.StringValue.SetValue("Another string");
            Assert.AreEqual(30, ValueSettingsKeys.IntValue.GetValue());
            Assert.AreEqual(9.1, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.AreEqual("Another string", ValueSettingsKeys.StringValue.GetValue());
        }

        [Test]
        public void TestSettingsValueChanged()
        {
            // We use an array to avoid a closure issue (resharper)
            int[] settingsChangedCount = { 0 };

            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            ValueSettingsKeys.IntValue.ChangesValidated += (s, e) => ++settingsChangedCount[0];
            ValueSettingsKeys.DoubleValue.ChangesValidated += (s, e) => ++settingsChangedCount[0];
            ValueSettingsKeys.StringValue.ChangesValidated += (s, e) => ++settingsChangedCount[0];

            ValueSettingsKeys.IntValue.SetValue(20);
            ValueSettingsKeys.DoubleValue.SetValue(6.5);
            ValueSettingsKeys.StringValue.SetValue("New string");
            SettingsGroup.CurrentProfile.ValidateSettingsChanges();
            Assert.AreEqual(3, settingsChangedCount[0]);
            settingsChangedCount[0] = 0;
        }

        [Test]
        public void TestSettingsList()
        {
            ListSettingsKeys.Initialize();
            var intList = ListSettingsKeys.IntList.GetValue();
            intList.Add(1);
            intList.Add(3);
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            doubleList.Remove(2.0);
            doubleList.RemoveAt(0);
            var stringList = ListSettingsKeys.StringList.GetValue();
            stringList.Insert(1, "String 1.5");
            stringList[2] = "String 2.0";

            intList = ListSettingsKeys.IntList.GetValue();
            Assert.That(intList, Is.EquivalentTo(new[] { 1, 3 }));
            doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.That(doubleList, Is.EquivalentTo(new[] { 9.0 }));
            stringList = ListSettingsKeys.StringList.GetValue();
            Assert.That(stringList, Is.EquivalentTo(new[] { "String 1", "String 1.5", "String 2.0", "String 3" }));
        }

        [Test]
        public void TestSettingsSaveAndLoad()
        {
            TestSettingsWrite();
            TestSettingsList();
            SettingsGroup.SaveSettingsProfile(SettingsGroup.CurrentProfile, TempPath("TestSettingsSaveAndLoad.txt"));
            SettingsGroup.LoadSettingsProfile(TempPath("TestSettingsSaveAndLoad.txt"), true);

            Assert.AreEqual(30, ValueSettingsKeys.IntValue.GetValue());
            Assert.AreEqual(9.1, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.AreEqual("Another string", ValueSettingsKeys.StringValue.GetValue());

            var intList = ListSettingsKeys.IntList.GetValue();
            Assert.That(intList, Is.EquivalentTo(new[] { 1, 3 }));
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.That(doubleList, Is.EquivalentTo(new[] { 9.0 }));
            var stringList = ListSettingsKeys.StringList.GetValue();
            Assert.That(stringList, Is.EquivalentTo(new[] { "String 1", "String 1.5", "String 2.0", "String 3" }));
        }

        const string TestSettingsLoadFileText =
@"!SettingsFile
Settings:
    Test/Lists/DoubleList:
        - 9
    Test/Lists/IntList:
        - 1
        - 3
    Test/Lists/StringList:
        - String 1
        - String 1.5
        - String 2
        - String 3
    Test/Simple/DoubleValue: 25.0
    Test/Simple/IntValue: 45
    Test/Simple/StringValue: 07/25/2004 18:18:00";

        [Test]
        public void TestSettingsLoad()
        {
            using (var writer = new StreamWriter(TempPath("TestSettingsLoad.txt")))
            {
                writer.Write(TestSettingsLoadFileText);
            }
            SettingsGroup.LoadSettingsProfile(TempPath("TestSettingsLoad.txt"), true);

            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            Assert.AreEqual(45, ValueSettingsKeys.IntValue.GetValue());
            Assert.AreEqual(25.0, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.AreEqual(new DateTime(2004, 7, 25, 18, 18, 00).ToString(CultureInfo.InvariantCulture), ValueSettingsKeys.StringValue.GetValue());
            var intList = ListSettingsKeys.IntList.GetValue();
            Assert.That(intList, Is.EquivalentTo(new[] { 1, 3 }));
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.That(doubleList, Is.EquivalentTo(new[] { 9.0 }));
            var stringList = ListSettingsKeys.StringList.GetValue();
            Assert.That(stringList, Is.EquivalentTo(new[] { "String 1", "String 1.5", "String 2", "String 3" }));
        }

        const string TestSettingsValueChangedOnLoadText =
@"!SettingsFile
Settings:
    Test/Lists/DoubleList: # Same as default
        - 2.0
        - 6.0
        - 9.0
    Test/Lists/IntList:
        - 1
    Test/Simple/DoubleValue: 3.14 # Same as default
    Test/Simple/IntValue: 45 # Different from default
    # String value unset";
        
        [Test]
        public void TestSettingsValueChangedOnLoad()
        {
            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            using (var writer = new StreamWriter(TempPath("TestSettingsValueChangedOnLoadText.txt")))
            {
                writer.Write(TestSettingsValueChangedOnLoadText);
            }

            int intValueChangeCount = 0;
            int doubleValueChangeCount = 0;
            int stringValueChangeCount = 0;
            int intListChangeCount = 0;
            int doubleListChangeCount = 0;
            int stringListChangeCount = 0;
            ValueSettingsKeys.IntValue.ChangesValidated += (s, e) => ++intValueChangeCount;
            ValueSettingsKeys.DoubleValue.ChangesValidated += (s, e) => ++doubleValueChangeCount;
            ValueSettingsKeys.StringValue.ChangesValidated += (s, e) => ++stringValueChangeCount;
            ListSettingsKeys.IntList.ChangesValidated += (s, e) => ++intListChangeCount;
            ListSettingsKeys.DoubleList.ChangesValidated += (s, e) => ++doubleListChangeCount;
            ListSettingsKeys.StringList.ChangesValidated += (s, e) => ++stringListChangeCount;

            SettingsGroup.LoadSettingsProfile(TempPath("TestSettingsValueChangedOnLoadText.txt"), true);
            SettingsGroup.CurrentProfile.ValidateSettingsChanges();

            Assert.AreEqual(1, intValueChangeCount);
            Assert.AreEqual(0, doubleValueChangeCount);
            Assert.AreEqual(0, stringValueChangeCount);
            Assert.AreEqual(1, intListChangeCount);
            Assert.AreEqual(0, doubleListChangeCount);
            Assert.AreEqual(0, stringListChangeCount);
        }

        const string TestSettingsLoadWrongTypeFileText =
@"!SettingsFile
Settings:
    Test/Lists/DoubleList:
        - String 1
        - String 2    
    Test/Lists/IntList: This is a string
    Test/Simple/DoubleValue: This is a string
    Test/Simple/IntValue:
        - String 1
        - String 2";

        [Test]
        public void TestSettingsLoadWrongType()
        {
            using (var writer = new StreamWriter(TempPath("TestSettingsLoadWrongType.txt")))
            {
                writer.Write(TestSettingsLoadWrongTypeFileText);
            }
            SettingsGroup.LoadSettingsProfile(TempPath("TestSettingsLoadWrongType.txt"), true);

            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            Assert.AreEqual(ValueSettingsKeys.IntValue.DefaultValue, ValueSettingsKeys.IntValue.GetValue());
            Assert.AreEqual(ValueSettingsKeys.DoubleValue.DefaultValue, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.AreEqual(ValueSettingsKeys.StringValue.DefaultValue, ValueSettingsKeys.StringValue.GetValue());
            var intList = ListSettingsKeys.IntList.GetValue();
            Assert.That(intList, Is.EquivalentTo(ListSettingsKeys.IntList.DefaultValue));
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.That(doubleList, Is.EquivalentTo(ListSettingsKeys.DoubleList.DefaultValue));
        }

        const string TestSettingsFileModifiedText1 =
@"!SettingsFile
Settings:
    Test/Simple/IntValue: 55";

        const string TestSettingsFileModifiedText2 =
@"!SettingsFile
Settings:
    Test/Simple/IntValue: 75";

        [Test]
        public void TestSettingsFileModified()
        {
            // NUnit does not support async tests so lets wrap this task into a synchronous operation
            var task = Task.Run(async () =>
                {
                    var tcs = new TaskCompletionSource<int>();
                    EventHandler<FileModifiedEventArgs> settingsModified = (s, e) => e.ReloadFile = true;
                    EventHandler<SettingsFileLoadedEventArgs> settingsLoaded = (s, e) => tcs.SetResult(0);
                    try
                    {
                        using (var writer = new StreamWriter(TempPath("TestSettingsFileModified.txt")))
                        {
                            writer.Write(TestSettingsFileModifiedText1);
                        }
                        SettingsGroup.LoadSettingsProfile(TempPath("TestSettingsFileModified.txt"), true);
                        SettingsGroup.CurrentProfile.MonitorFileModification = true;
                        SettingsGroup.CurrentProfile.FileModified += settingsModified;
                        ValueSettingsKeys.Initialize();
                        ListSettingsKeys.Initialize();
                        Assert.AreEqual(55, ValueSettingsKeys.IntValue.GetValue());

                        SettingsGroup.SettingsFileLoaded += settingsLoaded;

                        using (var writer = new StreamWriter(TempPath("TestSettingsFileModified.txt")))
                        {
                            writer.Write(TestSettingsFileModifiedText2);
                        }

                        // Gives some time to the file watcher to awake.
                        await tcs.Task;

                        Assert.AreEqual(75, ValueSettingsKeys.IntValue.GetValue());
                        SettingsGroup.SettingsFileLoaded -= settingsLoaded;
                    }
                    catch
                    {
                        SettingsGroup.SettingsFileLoaded -= settingsLoaded;
                    }
                });

            // Block while the task has not ended to ensure that no other test will start before this one ends.
            task.Wait();
        }
    }
}
