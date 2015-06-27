﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

using NUnit.Framework;
using SiliconStudio.Paradox.Animations;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering.Sprites;

namespace SiliconStudio.Paradox.Engine.Tests
{
    [TestFixture]
    public class TestSpriteAnimation : Game
    {
        [Test]
        public void DefaultValues()
        {
            Assert.AreEqual(30, SpriteAnimation.DefaultFramesPerSecond);
        }

        [Test]
        public void TestPauseResume()
        {
            var spriteComp = CreateSpriteComponent(15);

            SpriteAnimation.Play(spriteComp, 0, 10, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Pause(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.AreEqual(0, spriteComp.CurrentFrame);

            SpriteAnimation.Play(spriteComp, 0, 10, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.AreEqual(2, spriteComp.CurrentFrame);
            
            SpriteAnimation.Pause(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.AreEqual(2, spriteComp.CurrentFrame);

            SpriteAnimation.Resume(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.AreEqual(4, spriteComp.CurrentFrame);
        }

        [Test]
        public void TestStop()
        {
            var spriteComp = CreateSpriteComponent(20);

            SpriteAnimation.Play(spriteComp, 0, 1, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

            Assert.AreEqual(1, spriteComp.CurrentFrame); // check that is it correctly updated by default

            SpriteAnimation.Stop(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

            Assert.AreEqual(1, spriteComp.CurrentFrame); // check that current frame does not increase any more

            SpriteAnimation.Play(spriteComp, 2, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.AreEqual(2, spriteComp.CurrentFrame); // check that frame is correctly set to first animation frame

            SpriteAnimation.Play(spriteComp, 2, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 5, 6, AnimationRepeatMode.PlayOnce, 1);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.AreEqual(2, spriteComp.CurrentFrame); // check that is it correctly updated by default

            SpriteAnimation.Stop(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.AreEqual(2, spriteComp.CurrentFrame); // check that queue is correctly reset

            SpriteAnimation.Play(spriteComp, 2, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)));

            Assert.AreEqual(3, spriteComp.CurrentFrame); // check that queue is correctly reset

            SpriteAnimation.Stop(spriteComp);
            SpriteAnimation.Queue(spriteComp, 5, 6, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.AreEqual(5, spriteComp.CurrentFrame); // check that indices are correctly reseted during stop
        }

        [Test]
        public void TestPlay()
        {
            var spriteComp = CreateSpriteComponent(20);

            SpriteAnimation.Play(spriteComp, 1, 5, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.AreEqual(1, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

            Assert.AreEqual(2, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)));

            Assert.AreEqual(2, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(9)));

            Assert.AreEqual(5, spriteComp.CurrentFrame); // check that it does not exceed last frame

            SpriteAnimation.Play(spriteComp, 5, 7, AnimationRepeatMode.LoopInfinite, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.AreEqual(5, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)));

            Assert.AreEqual(5, spriteComp.CurrentFrame); // check looping

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4)));

            Assert.AreEqual(6, spriteComp.CurrentFrame); // check looping

            SpriteAnimation.Play(spriteComp, new[] { 9, 5, 10, 9 }, AnimationRepeatMode.LoopInfinite, 1);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));
            Assert.AreEqual(9, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(5, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(10, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(9, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(9, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(5, spriteComp.CurrentFrame);

            // check queue reset
            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, 7, 8, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4)));

            Assert.AreEqual(8, spriteComp.CurrentFrame); // check queue reset

            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, new[] { 7, 8 }, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4)));

            Assert.AreEqual(8, spriteComp.CurrentFrame); // check queue reset

            SpriteAnimation.Play(spriteComp, 0, 0, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, 7, 8, AnimationRepeatMode.PlayOnce, 1, false);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));

            Assert.AreEqual(4, spriteComp.CurrentFrame); // check queue no reset

            SpriteAnimation.Play(spriteComp, 0, 0, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, new[] { 7, 8 }, AnimationRepeatMode.PlayOnce, 1, false);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));

            Assert.AreEqual(4, spriteComp.CurrentFrame); // check queue no reset

            // check default fps speed
            SpriteAnimation.Play(spriteComp, 0, 15, AnimationRepeatMode.PlayOnce);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0.51), TimeSpan.FromSeconds(0.51)));
            Assert.AreEqual(15, spriteComp.CurrentFrame); // check queue no reset
        }

        [Test]
        public void TestQueue()
        {
            var spriteComp = CreateSpriteComponent(20);

            // check queue before play
            SpriteAnimation.Queue(spriteComp, 1, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.AreEqual(1, spriteComp.CurrentFrame);

            // check queue sequence
            SpriteAnimation.Queue(spriteComp, new[] { 5, 9, 4 }, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, new[] { 6 }, AnimationRepeatMode.LoopInfinite, 1);
            SpriteAnimation.Queue(spriteComp, new[] { 7 }, AnimationRepeatMode.PlayOnce, 1);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));
            Assert.AreEqual(3, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(5, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));
            Assert.AreEqual(4, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.AreEqual(6, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));
            Assert.AreEqual(6, spriteComp.CurrentFrame); 
        }

        private static SpriteComponent CreateSpriteComponent(int nbOfFrames)
        {
            var spriteGroup = new SpriteGroup { Images = new List<Sprite>() };
            var sprite = new SpriteComponent { SpriteProvider = new SpriteFromSpriteGroup { SpriteGroup = spriteGroup } };

            // add a few sprites
            for (int i = 0; i < nbOfFrames; i++)
            {
                spriteGroup.Images.Add(new Sprite(Guid.NewGuid().ToString()));
            }

            return sprite;
        }
    }
}