﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

using NUnit.Framework;
using SiliconStudio.TextureConverter.Requests;
using SiliconStudio.TextureConverter.TexLibraries;

namespace SiliconStudio.TextureConverter.Tests
{
    [TestFixture]
    class PvrttTexLibTest
    {
        PvrttTexLib library;

        [TestFixtureSetUp]
        public void TestSetUp()
        {
            library = new PvrttTexLib();
            Assert.IsFalse(library.SupportBGRAOrder());
        }

        [TestFixtureTearDown]
        public void TestTearDown()
        {
            library.Dispose();
        }

        [TestCase("TextureArray_WMipMaps_BGRA8888.dds")]
        [TestCase("TextureCube_WMipMaps_BGRA8888.dds")]
        public void StartLibraryTest(string file)
        {
            TexImage image = new TexImage();

            var dxtLib = new DxtTexLib();
            dxtLib.Execute(image, new LoadingRequest(TestTools.InputTestFolder + file, false));
            image.CurrentLibrary = dxtLib;
            dxtLib.EndLibrary(image);

            TexLibraryTest.StartLibraryTest(image, library);

            image.Dispose();
        }


        [Test]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "TextureArray_WMipMaps_PVRTC2_4bpp.pvr");
            Assert.IsTrue(library.CanHandleRequest(image, new CompressingRequest(Paradox.Graphics.PixelFormat.PVRTC_II_4bpp)));
            Assert.IsTrue(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.IsTrue(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", false)));
            Assert.IsTrue(library.CanHandleRequest(image, new MipMapsGenerationRequest(Filter.MipMapGeneration.Linear)));
            Assert.IsTrue(library.CanHandleRequest(image, new NormalMapGenerationRequest(0.5f)));
            Assert.IsTrue(library.CanHandleRequest(image, new SwitchingBRChannelsRequest()));
            Assert.IsTrue(library.CanHandleRequest(image, new FlippingRequest(Orientation.Horizontal)));
            Assert.IsTrue(library.CanHandleRequest(image, new FixedRescalingRequest(512, 512, Filter.Rescaling.Nearest)));
            Assert.IsTrue(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", 0)));
            Assert.IsFalse(library.CanHandleRequest(image, new GammaCorrectionRequest(1)));
            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_PVRTC2_4bpp.pvr")]
        [TestCase("TextureCube_WMipMaps_PVRTC2_4bpp.pvr")]
        public void DecompressTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.DecompressTest(image, library);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Paradox.Graphics.PixelFormat.PVRTC_II_4bpp)]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr", Paradox.Graphics.PixelFormat.PVRTC_II_4bpp)]
        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Paradox.Graphics.PixelFormat.ETC2_RGBA)]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr", Paradox.Graphics.PixelFormat.ETC2_RGBA)]
        public void CompressTest(string file, Paradox.Graphics.PixelFormat format)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.CompressTest(image, library, format);

            image.Dispose();
        }


        [TestCase("TextureArray_WOMipMaps_PVRTC2_4bpp.pvr", Filter.MipMapGeneration.Box)]
        [TestCase("TextureCube_WOMipMaps_PVRTC2_4bpp.pvr", Filter.MipMapGeneration.Cubic)]
        public void GenerateMipMapTest(string file, Filter.MipMapGeneration filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.GenerateMipMapTest(image, library, filter);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", "PvrttTexLib_GenerateNormalMapTest_TextureArray_WOMipMaps_PVRTC2_4bpp.pvr")]
        [TestCase("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", "PvrttTexLib_GenerateNormalMapTest_TextureCube_WOMipMaps_PVRTC2_4bpp.pvr")]
        public void GenerateNormalMapTest(string file, string outFile)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.GenerateNormalMapTest(image, library);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bicubic)]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bilinear)]
        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Nearest)]
        public void FixedRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FixedRescaleTest(image, library, filter);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bicubic)]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bilinear)]
        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Box)]
        public void FactorRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FactorRescaleTest(image, library, filter);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_PVRTC2_4bpp.pvr")]
        [TestCase("TextureCube_WMipMaps_PVRTC2_4bpp.pvr")]
        public void ExportTest(String file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportTest(image, library, file);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", 16)]
        [TestCase("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", 512)]
        [TestCase("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", 8)]
        [TestCase("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", 4)]
        public void ExportMinMipMapTest(String file, int minMipMapSize)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportMinMipMapTest(image, library, minMipMapSize);

            image.Dispose();
        }

        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr")]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr")]
        public void SwitchChannelsTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.SwitchChannelsTest(image, library);

            image.Dispose();
        }

        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr", Orientation.Horizontal)]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr", Orientation.Vertical)]
        public void FlipTest(String file, Orientation orientation)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FlipTest(image, library, orientation);

            image.Dispose();
        }

        [TestCase("TextureArray_WMipMaps_RGBA8888.pvr")]
        [TestCase("TextureCube_WMipMaps_RGBA8888.pvr")]
        public void PreMultiplyAlphaTest(String file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.PreMultiplyAlphaTest(image, library);

            image.Dispose();
        }

    }
}
