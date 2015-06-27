﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.IO;

using NUnit.Framework;
using SiliconStudio.TextureConverter.Requests;
using SiliconStudio.TextureConverter.TexLibraries;

namespace SiliconStudio.TextureConverter.Tests
{
    [TestFixture]
    class FiTexLibTest
    {
        FITexLib library;

        [TestFixtureSetUp]
        public void TestSetUp()
        {
            library = new FITexLib();
            Assert.IsTrue(library.SupportBGRAOrder());
        }

        [TestFixtureTearDown]
        public void TestTearDown()
        {
            library.Dispose();
        }

        [TestCase("duck.jpg")]
        [TestCase("stones.png")]
        [TestCase("snap1.psd")]
        public void StartLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.StartLibraryTest(image, library);

            image.Dispose();
        }


        [TestCase("duck.jpg")]
        [TestCase("stones.png")]
        public void EndLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            library.EndLibrary(image);
            image.CurrentLibrary = null;

            image.Dispose();
        }

        [TestCase("duck.jpg")]
        [TestCase("stones.png")]
        public void CorrectGammaTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.CorrectGammaTest(image, library);

            image.Dispose();
        }

        [TestCase("duck.jpg", Orientation.Horizontal)]
        [TestCase("stones.png", Orientation.Vertical)]
        public void FlipTest(string file, Orientation orientation)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FlipTest(image, library, orientation);

            image.Dispose();
        }

        [TestCase("duck.jpg", Filter.Rescaling.Bicubic)]
        [TestCase("stones.png", Filter.Rescaling.Bilinear)]
        [TestCase("duck.jpg", Filter.Rescaling.Box)]
        [TestCase("stones.png", Filter.Rescaling.BSpline)]
        [TestCase("duck.jpg", Filter.Rescaling.CatmullRom)]
        [TestCase("stones.png", Filter.Rescaling.Lanczos3)]
        public void FixedRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FixedRescaleTest(image, library, filter);

            image.Dispose();
        }

        [Test]
        public void FixedRescale3DTest()
        {
            DxtTexLib lib = new DxtTexLib();
            TexImage image = new TexImage();
            lib.Execute(image, new LoadingRequest(TestTools.InputTestFolder+"Texture3D_WMipMaps_BGRA8888.dds", false));
            image.Name = "Texture3D_WMipMaps_BGRA8888.dds";
            lib.EndLibrary(image);
            library.StartLibrary(image);
            image.CurrentLibrary = library;
            TexLibraryTest.FactorRescaleTest(image, library, Filter.Rescaling.Lanczos3);

            image.Dispose();
        }


        [TestCase("stones.png", Filter.Rescaling.Bicubic)]
        [TestCase("stones.png", Filter.Rescaling.Bilinear)]
        [TestCase("stones.png", Filter.Rescaling.Box)]
        [TestCase("stones.png", Filter.Rescaling.BSpline)]
        [TestCase("stones.png", Filter.Rescaling.CatmullRom)]
        [TestCase("stones.png", Filter.Rescaling.Lanczos3)]
        public void FactorRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FactorRescaleTest(image, library, filter);
            
            image.Dispose();
        }


        [TestCase("duck.jpg")]
        [TestCase("stones.png")]
        public void SwitchChannelsTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.SwitchChannelsTest(image, library);

            image.Dispose();
        }


        [TestCase("TextureArray_WMipMaps_BGRA8888", ".dds", 0)]
        [TestCase("TextureArray_WMipMaps_BGRA8888", ".dds", 16)]
        public void ExportArrayTest(string fileName, string extension, int minMipMapSize)
        {
            DxtTexLib lib = new DxtTexLib();
            TexImage image = new TexImage();
            lib.Execute(image, new LoadingRequest(TestTools.InputTestFolder + fileName + extension, false));
            lib.EndLibrary(image);
            library.StartLibrary(image);

            library.Execute(image, new ExportRequest(TestTools.TempFolder + "FITexLibTest_ExportArrayTest_" + fileName + ".png", minMipMapSize));

            int ct = 0;
            for (int i = 0; i < image.ArraySize; ++i)
            {
                for (int j = 0; j < image.MipmapCount; ++j)
                {
                    if (image.SubImageArray[ct].Height < minMipMapSize || image.SubImageArray[ct].Width < minMipMapSize)
                        break;
                    string file = TestTools.TempFolder + "FITexLibTest_ExportArrayTest_" + fileName + "-ind_" + i + "-mip_" + j + ".png";
                    Assert.IsTrue(File.Exists(file));

                    //Console.WriteLine("FITexLibTest_ExportArrayTest_" + minMipMapSize + "_" + fileName + "-ind_" + i + "-mip_" + j + ".png" + "." + TestTools.ComputeSHA1(file));
                    Assert.IsTrue(TestTools.ComputeSHA1(file).Equals(TestTools.GetInstance().Checksum["FITexLibTest_ExportArrayTest_" + minMipMapSize + "_" + fileName + "-ind_" + i + "-mip_" + j + ".png"]));
                    File.Delete(file);
                    ++ct;
                }
            }

            image.Dispose();
        }


        [Test]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "stones.png");
            Assert.IsFalse(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.IsTrue(library.CanHandleRequest(image, new FixedRescalingRequest(0, 0, Filter.Rescaling.Bilinear)));
            Assert.IsTrue(library.CanHandleRequest(image, new SwitchingBRChannelsRequest()));
            Assert.IsTrue(library.CanHandleRequest(image, new FlippingRequest(Orientation.Vertical)));
            Assert.IsTrue(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_BC3.png", false)));
            Assert.IsTrue(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_BC3.png", 0)));
            Assert.IsTrue(library.CanHandleRequest(image, new GammaCorrectionRequest(0)));
            image.Dispose();
        }

    }
}
