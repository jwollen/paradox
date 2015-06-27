﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Threading;

using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Graphics.Font;

namespace SiliconStudio.Paradox.Graphics.Tests
{
    /// <summary>
    /// Test the class <see cref="FontManager"/>
    /// </summary>
    [TestFixture]
    public class TestFontManager
    {
        [TestFixtureSetUp]
        public void StartGame()
        {
            Game.InitializeAssetDatabase();
        }

        [Test]
        public void TestCreationDisposal()
        {
            FontManager fontManager = null;
            Assert.DoesNotThrow(() => fontManager = new FontManager());
            Assert.DoesNotThrow(() => fontManager.Dispose());
        }
        
        [Test]
        public void TestDoesFontContains()
        {
            var fontManager = new FontManager();
            Assert.IsTrue(fontManager.DoesFontContains("Arial", FontStyle.Regular, 'a'));
            Assert.IsFalse(fontManager.DoesFontContains("Arial", FontStyle.Regular, '都'));
            fontManager.Dispose();
        }

        [Test]
        public void TestGetFontInfo()
        {
            var fontManager = new FontManager();

            float lineSpacing = 0;
            float baseLine = 0;
            float width = 0;
            float height = 0;
            Assert.DoesNotThrow(() => fontManager.GetFontInfo("Risaltyp_024", FontStyle.Regular, out lineSpacing, out baseLine, out width, out height));
            Assert.AreEqual(4444f / 4096f, lineSpacing);
            Assert.AreEqual(3233f / 4096f, baseLine);
            Assert.AreEqual(3657f / 4096f, width);
            Assert.AreEqual(4075f / 4096f, height);

            fontManager.Dispose();
        }

        [Test]
        public void TestGenerateBitmap()
        {
            var fontManager = new FontManager();
            const int waitTime = 250;
            const int defaultSize = 4;

            // test that a simple bitmap generation success
            var characterA = new CharacterSpecification('a', "Arial", new Vector2(1.73f, 3.57f), FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterA, false);
            WaitAndCheck(characterA, waitTime);
            Assert.AreEqual(4, characterA.Bitmap.Width);
            Assert.AreEqual(6, characterA.Bitmap.Rows);
            
            // test that rendering an already existing character to a new size works
            var characterA2 = new CharacterSpecification('a', "Arial", 10f * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterA2, false);
            WaitAndCheck(characterA2, waitTime);
            Assert.AreNotEqual(2, characterA2.Bitmap.Width);
            Assert.AreNotEqual(4, characterA2.Bitmap.Rows);

            // test that trying to render a character that does not exist does not crash the system
            var characterTo = new CharacterSpecification('都', "Arial", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            var characterB = new CharacterSpecification('b', "Arial", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterTo, false);
            fontManager.GenerateBitmap(characterB, false);
            WaitAndCheck(characterB, 2 * waitTime);
            Assert.AreEqual(null, characterTo.Bitmap);

            // test that trying to render a character that does not exist does not crash the system
            var characterC = new CharacterSpecification('c', "Arial", -1 * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            var characterD = new CharacterSpecification('d', "Arial", defaultSize * Vector2.One, FontStyle.Regular, FontAntiAliasMode.Default);
            fontManager.GenerateBitmap(characterC, false);
            fontManager.GenerateBitmap(characterD, false);
            WaitAndCheck(characterD, 2 * waitTime);
            Assert.AreEqual(null, characterC.Bitmap);
            
            fontManager.Dispose();
        }

        private void WaitAndCheck(CharacterSpecification character, int sleepTime)
        {
            Thread.Sleep(sleepTime);
            Assert.AreNotEqual(null, character.Bitmap);
        }
    }
}