﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using NUnit.Framework;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Serializers;

namespace SiliconStudio.Core.Tests
{
    [TestFixture]
    [DataSerializerGlobal(null, typeof(KeyValuePair<int, int>))]
    public class TestStore
    {
        [Test]
        public void ListSimple()
        {
            using (var tempFile = new TemporaryFile())
            using (var store2 = new ListStore<int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            using (var store1 = new ListStore<int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            {
                store1.UseTransaction = true;

                // Add a value to store2 and saves it
                store2.AddValue(1);
                store2.Save();

                // Add a value to store1 without saving
                store1.AddValue(2);
                Assert.AreEqual(new[] { 2 }, store1.GetValues());

                // Check that store1 contains value from store2 first
                store1.LoadNewValues();
                Assert.AreEqual(new[] { 1, 2 }, store1.GetValues());

                // Save and check that results didn't change
                store1.Save();
                Assert.AreEqual(new[] { 1, 2 }, store1.GetValues());
            }
        }

        [Test]
        public void DictionarySimple()
        {
            using (var tempFile = new TemporaryFile())
            using (var store1 = new DictionaryStore<int, int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            using (var store2 = new DictionaryStore<int, int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            {
                store1.UseTransaction = true;

                // Check successive sets
                store1[1] = 1;
                Assert.That(store1[1], Is.EqualTo(1));

                store1[1] = 2;
                Assert.That(store1[1], Is.EqualTo(2));

                // Check saving (before and after completion)
                store1.Save();
                Assert.That(store1[1], Is.EqualTo(2));
                Assert.That(store1[1], Is.EqualTo(2));

                // Check set after save
                store1[1] = 3;
                Assert.That(store1[1], Is.EqualTo(3));

                // Check loading from another store
                store2.LoadNewValues();
                Assert.That(store2[1], Is.EqualTo(2));

                // Concurrent changes
                store1[1] = 5;
                store2[1] = 6;
                // Write should be scheduled for save immediately since dictionaryStore2 doesn't use transaction
                store2[2] = 6;

                // Check intermediate state (should get new value for 2, but keep intermediate non-saved value for 1)
                store1.LoadNewValues();
                Assert.That(store1[1], Is.EqualTo(5));
                Assert.That(store1[2], Is.EqualTo(6));

                // Check after save/reload, both stores should be synchronized
                store1.Save();
                store2.LoadNewValues();
                Assert.That(store1[1], Is.EqualTo(store2[1]));
                Assert.That(store1[2], Is.EqualTo(store2[2]));
            }
        }
    }
}