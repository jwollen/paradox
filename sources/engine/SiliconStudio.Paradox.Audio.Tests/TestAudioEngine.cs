﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

using NUnit.Framework;

using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Engine;

namespace SiliconStudio.Paradox.Audio.Tests
{
    /// <summary>
    /// Tests for <see cref="AudioEngine"/>
    /// </summary>
    [TestFixture]
    public class TestAudioEngine
    {
        [TestFixtureSetUp]
        public void Initialize()
        {
            Game.InitializeAssetDatabase();
        }

        /// <summary>
        /// Test that audio engine initializes and disposes correctly without exceptions.
        /// If no audio hardware is activated or no output are connected, the AudioInitializationException should be thrown.
        /// </summary>
        [Test]
        public void ContextInitialization()
        {
            try
            {
                var engine = new AudioEngine();
                engine.Dispose();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is AudioInitializationException, "Audio engine failed to initialize but did not throw AudioInitializationException");
            }
        }

        /// <summary>
        /// Test that there are no crashes when shutting down the engine with not disposed soundEffects.
        /// </summary>
        [Test]
        public void TestDispose()
        {
            var crossDisposedEngine = new AudioEngine(); 
            var engine = new AudioEngine();
            crossDisposedEngine.Dispose(); // Check there no Dispose problems with sereval cross-disposed instances. 

            // Create some SoundEffects
            SoundEffect soundEffect;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                soundEffect = SoundEffect.Load(engine, wavStream);
            }
            SoundEffect dispSoundEffect;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                dispSoundEffect = SoundEffect.Load(engine, wavStream);
            }
            dispSoundEffect.Dispose();

            var soundEffectInstance = soundEffect.CreateInstance();
            var dispInstance = soundEffect.CreateInstance();
            dispInstance.Dispose();

            // Create some SoundMusics.
            var soundMusic1 = SoundMusic.Load(engine, AssetManager.FileProvider.OpenStream("MusicBip", VirtualFileMode.Open, VirtualFileAccess.Read));
            var soundMusic2 = SoundMusic.Load(engine, AssetManager.FileProvider.OpenStream("MusicToneA", VirtualFileMode.Open, VirtualFileAccess.Read));
            soundMusic2.Dispose();

            // Create some dynamicSounds.
            var generator = new SoundGenerator();
            var dynSound1 = new DynamicSoundEffectInstance(engine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
            var dynSound2 = new DynamicSoundEffectInstance(engine, 20000, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
            dynSound1.Play();
            dynSound1.SubmitBuffer(generator.Generate(44100, new[]{ 1000f }, 1, 120000));
            dynSound2.Dispose();

            // Start playing some
            soundEffectInstance.Play();
            soundMusic1.Play();

            for (int i = 0; i < 10; i++)
            {
                engine.Update();
                Utilities.Sleep(5);
            }

            Assert.DoesNotThrow(engine.Dispose, "AudioEngine crashed during disposal.");
            Assert.IsTrue(soundEffect.IsDisposed, "SoundEffect is not disposed.");
            Assert.Throws<InvalidOperationException>(engine.Dispose, "AudioEngine did not threw invalid operation exception.");
            Assert.AreEqual(SoundPlayState.Stopped, soundEffectInstance.PlayState, "SoundEffectInstance has not been stopped properly.");
            Assert.IsTrue(soundEffectInstance.IsDisposed, "SoundEffectInstance has not been disposed properly.");
            Assert.AreEqual(SoundPlayState.Stopped, soundMusic1.PlayState, "soundMusic1 has not been stopped properly.");
            Assert.IsTrue(soundMusic1.IsDisposed, "soundMusic1 has not been disposed properly.");
            //Assert.AreEqual(SoundPlayState.Stopped, dynSound1.PlayState, "The dynamic sound 1 has not been stopped correctly.");
            //Assert.IsTrue(dynSound1.IsDisposed, "The dynamic sound 1 has not been disposed correctly.");
        }

        /// <summary>
        /// Test the behavior of <see cref="AudioEngine.GetLeastSignificativeSoundEffect"/>
        /// </summary>
        [Test]
        public void TestGetLeastSignificativeSoundEffect()
        {
            var engine = new AudioEngine();

            //////////////////////////////////////////
            // 1. Test that it returns null by default
            Assert.AreEqual(null, engine.GetLeastSignificativeSoundEffect());

            // create a sound effect
            SoundEffect soundEffect;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
                soundEffect = SoundEffect.Load(engine, wavStream);

            /////////////////////////////////////////////////////////////////////
            // 2. Test that is returns null when there is no playing sound effect
            Assert.AreEqual(null, engine.GetLeastSignificativeSoundEffect());

            /////////////////////////////////////////////////////////////////////////////////
            // 3. Test that is returns null when there is no not looped sound effect playing
            soundEffect.IsLooped = true;
            soundEffect.Play();
            Assert.AreEqual(null, engine.GetLeastSignificativeSoundEffect());

            ////////////////////////////////////////////////////////////////////////////
            // 4. Test that the sound effect is returned if not playing and not looped
            soundEffect.Stop();
            soundEffect.IsLooped = false;
            soundEffect.Play();
            Assert.AreEqual(soundEffect, engine.GetLeastSignificativeSoundEffect());

            // create another longer sound effect
            SoundEffect longerSoundEffect;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectStereo", VirtualFileMode.Open, VirtualFileAccess.Read))
                longerSoundEffect = SoundEffect.Load(engine, wavStream);

            ///////////////////////////////////////////////////////////////////////
            // 5. Test that the longest sound is returned if it is the only playing
            soundEffect.Stop();
            longerSoundEffect.Play();
            Assert.AreEqual(longerSoundEffect, engine.GetLeastSignificativeSoundEffect());

            //////////////////////////////////////////////////////////////
            // 6. Test that the shortest sound is returned if both playing
            longerSoundEffect.Play();
            soundEffect.Play();
            Assert.AreEqual(soundEffect, engine.GetLeastSignificativeSoundEffect());

            //////////////////////////////////////////////////////////////////////
            // 7. Test that the longest sound is returned if the other is looped
            soundEffect.Stop();
            soundEffect.IsLooped = true;
            soundEffect.Play();
            longerSoundEffect.Play();
            Assert.AreEqual(longerSoundEffect, engine.GetLeastSignificativeSoundEffect());

            engine.Dispose();
        }
        
        /// <summary>
        /// Test the behavior of <see cref="AudioEngine.State"/>
        /// </summary>
        [Test]
        public void TestState()
        {
            // test initial state
            var engine = new AudioEngine();
            Assert.AreEqual(AudioEngineState.Running, engine.State);

            // test state after pause
            engine.PauseAudio();
            Assert.AreEqual(AudioEngineState.Paused, engine.State);

            // test state after resume
            engine.ResumeAudio();
            Assert.AreEqual(AudioEngineState.Running, engine.State);

            // test state after dispose
            engine.Dispose();
            Assert.AreEqual(AudioEngineState.Disposed, engine.State);

            // test that state remains dispose
            engine.PauseAudio();
            Assert.AreEqual(AudioEngineState.Disposed, engine.State);
            engine.ResumeAudio();
            Assert.AreEqual(AudioEngineState.Disposed, engine.State);
        }

        /// <summary>
        /// Test the behavior of <see cref="AudioEngine.PauseAudio"/>
        /// </summary>
        [Test]
        public void TestPauseAudio()
        {
            var engine = new AudioEngine();
            
            // create a sound effect instance
            SoundEffect soundEffect;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
                soundEffect = SoundEffect.Load(engine, wavStream);
            var wave1Instance = soundEffect.CreateInstance();

            // create a music instance
            var music = SoundMusic.Load(engine, AssetManager.FileProvider.OpenStream("MusicFishLampMp3", VirtualFileMode.Open, VirtualFileAccess.Read));

            // check state
            engine.PauseAudio();
            Assert.AreEqual(AudioEngineState.Paused, engine.State);

            // check that existing instance can not be played
            wave1Instance.Play();
            Assert.AreEqual(SoundPlayState.Stopped, wave1Instance.PlayState);
            music.Play();
            Assert.AreEqual(SoundPlayState.Stopped, music.PlayState);
            TestAudioUtilities.ActiveAudioEngineUpdate(engine, 1000); // listen that nothing comes out

            // create a new sound effect
            SoundEffect soundEffectStereo;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectStereo", VirtualFileMode.Open, VirtualFileAccess.Read))
                soundEffectStereo = SoundEffect.Load(engine, wavStream);

            // check that a new instance can not be played
            soundEffectStereo.Play();
            Assert.AreEqual(SoundPlayState.Stopped, soundEffectStereo.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that a stopped sound stay stopped
            engine.ResumeAudio();
            soundEffectStereo.Stop();
            engine.PauseAudio();
            Assert.AreEqual(SoundPlayState.Stopped, soundEffectStereo.PlayState);

            // check that a paused sound stay paused
            engine.ResumeAudio();
            soundEffectStereo.Play();
            soundEffectStereo.Pause();
            engine.PauseAudio();
            Assert.AreEqual(SoundPlayState.Paused, soundEffectStereo.PlayState);

            // check that a playing sound is paused
            engine.ResumeAudio();
            wave1Instance.Play();
            soundEffectStereo.Play();
            music.Play();
            engine.PauseAudio();
            Assert.AreEqual(SoundPlayState.Paused, wave1Instance.PlayState);
            Assert.AreEqual(SoundPlayState.Paused, soundEffectStereo.PlayState);
            Assert.AreEqual(SoundPlayState.Paused, music.PlayState);
            TestAudioUtilities.ActiveAudioEngineUpdate(engine, 1000); // listen that nothing comes out

            // check that stopping a sound while paused is possible
            engine.PauseAudio();
            soundEffectStereo.Stop();
            Assert.AreEqual(SoundPlayState.Stopped, soundEffectStereo.PlayState);

            // check that disposing a sound while paused is possible
            engine.PauseAudio();
            music.Dispose();
            Assert.IsTrue(music.IsDisposed);

            soundEffect.Dispose();
            soundEffectStereo.Dispose();
        }

        /// <summary>
        /// Test the behavior of <see cref="AudioEngine.ResumeAudio"/>
        /// </summary>
        [Test]
        public void TestResumeAudio()
        {
            var engine = new AudioEngine();

            // create a sound effect instance
            SoundEffect soundEffect;
            using (var wavStream = AssetManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
                soundEffect = SoundEffect.Load(engine, wavStream);
            var wave1Instance = soundEffect.CreateInstance();

            // create a music instance
            var music = SoundMusic.Load(engine, AssetManager.FileProvider.OpenStream("MusicFishLampMp3", VirtualFileMode.Open, VirtualFileAccess.Read));
            
            // check that resume do not play stopped instances
            engine.PauseAudio();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Stopped, music.PlayState);
            Assert.AreEqual(SoundPlayState.Stopped, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that user paused music does not resume
            wave1Instance.Play();
            wave1Instance.Pause();
            engine.PauseAudio();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Paused, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that sounds paused by PauseAudio are correctly restarted
            wave1Instance.Play();
            music.Play();
            engine.PauseAudio();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Playing, wave1Instance.PlayState);
            Assert.AreEqual(SoundPlayState.Playing, music.PlayState);
            TestAudioUtilities.ActiveAudioEngineUpdate(engine, 3000);// listen that the sound comes out
            music.Stop();
            TestAudioUtilities.ActiveAudioEngineUpdate(engine, 100);// stop the music

            // check that a sound stopped during the pause does not play during the resume
            wave1Instance.Play();
            engine.PauseAudio();
            wave1Instance.Stop();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Stopped, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that a sound played during the pause do not play during the resume
            engine.PauseAudio();
            wave1Instance.Play();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Stopped, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that a two calls to resume do not have side effects (1)
            wave1Instance.Play();
            engine.PauseAudio();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Playing, wave1Instance.PlayState);
            Utilities.Sleep(2000); // wait that the sound is finished
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Stopped, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that a two calls to resume do not have side effects (2)
            wave1Instance.Play();
            engine.PauseAudio();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Playing, wave1Instance.PlayState);
            wave1Instance.Pause();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Paused, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out

            // check that a several calls to pause/play do not have side effects
            wave1Instance.Play();
            engine.PauseAudio();
            engine.ResumeAudio();
            engine.PauseAudio();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Playing, wave1Instance.PlayState);
            Utilities.Sleep(2000); // listen that the sound comes out

            // check that the sound is not played if disposed
            wave1Instance.Play();
            engine.PauseAudio();
            wave1Instance.Dispose();
            engine.ResumeAudio();
            Assert.AreEqual(SoundPlayState.Stopped, wave1Instance.PlayState);
            Utilities.Sleep(1000); // listen that nothing comes out
            
            music.Dispose();
            soundEffect.Dispose();
        }
    }
}
