﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.ComputeEffect;
using SiliconStudio.Paradox.Rendering.Images;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics.Regression;

namespace SiliconStudio.Paradox.Graphics.Tests
{
    /// <summary>
    /// The the pass 2 of lambertian prefiltering SH
    /// </summary>
    public class TestLambertPrefilteringSHPass2 : GraphicsTestBase
    {
        private const int Order = 2;

        private const int NbOfCoeffs = Order * Order;

        private const int NbOfSums = 8;

        private Int2 nbOfGroups = new Int2(2, 3);

        private Vector4[] inputBufferData;

        private ComputeEffectShader pass2;

        private Buffer outputBuffer;

        private Buffer<Vector4> inputBuffer;

        private readonly bool assertResults;

        private readonly Int2 screenSize = new Int2(1200, 900);

        public TestLambertPrefilteringSHPass2() : this(true)
        {
        }

        public TestLambertPrefilteringSHPass2(bool assertResults)
        {
            this.assertResults = assertResults;
            GraphicsDeviceManager.PreferredBackBufferWidth = screenSize.X;
            GraphicsDeviceManager.PreferredBackBufferHeight = screenSize.Y;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            CreateBufferData();

            inputBuffer = Buffer.Typed.New(GraphicsDevice, inputBufferData, PixelFormat.R32G32B32A32_Float, true);
            outputBuffer = Buffer.Typed.New(GraphicsDevice, NbOfCoeffs * nbOfGroups.X * nbOfGroups.Y, PixelFormat.R32G32B32A32_Float, true);

            var context = RenderContext.GetShared(Services);
            pass2 = new ComputeEffectShader(context) { ShaderSourceName = "LambertianPrefilteringSHEffectPass2", };
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            pass2.ThreadNumbers = new Int3(NbOfSums, 1, 1);
            pass2.ThreadGroupCounts = new Int3(nbOfGroups.X, nbOfGroups.Y, NbOfCoeffs);
            pass2.Parameters.Set(LambertianPrefilteringSHParameters.BlockSize, NbOfSums);
            pass2.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, Order);
            pass2.Parameters.Set(LambertianPrefilteringSHPass2Keys.InputBuffer, inputBuffer);
            pass2.Parameters.Set(LambertianPrefilteringSHPass2Keys.OutputBuffer, outputBuffer);
            pass2.Draw();

            // Get the data out of the final buffer
            var finalsValues = outputBuffer.GetData<Vector4>();

            // performs last possible additions, normalize the result and store it in the SH
            var result = new Vector4[NbOfCoeffs];
            for (var c = 0; c < NbOfCoeffs; c++)
            {
                var coeff = Vector4.Zero;
                for (var f = 0; f < nbOfGroups.X * nbOfGroups.Y; ++f)
                {
                    coeff += finalsValues[NbOfCoeffs * f + c];
                }
                result[c] = coeff;
            }

            var nbOfTerms = NbOfSums * nbOfGroups.X * nbOfGroups.Y;
            var valueSum = (nbOfTerms - 1) * nbOfTerms / 2;

            if (assertResults)
            {
                Assert.AreEqual(new Vector4(valueSum, 0, 0, 0), result[0]);
                Assert.AreEqual(new Vector4(0, 2 * valueSum, 0, 0), result[1]);
                Assert.AreEqual(new Vector4(0, 0, 3 * valueSum, 0), result[2]);
                Assert.AreEqual(new Vector4(0, 0, 0, 4 * valueSum), result[3]);
            }
        }

        private void CreateBufferData()
        {
            inputBufferData = new Vector4[NbOfCoeffs * NbOfSums * nbOfGroups.X * nbOfGroups.Y];

            // initialize values
            for (int i = 0; i < NbOfCoeffs; i++)
            {
                for (int u = 0; u < NbOfSums * nbOfGroups.X * nbOfGroups.Y; ++u)
                {
                    var value = Vector4.Zero;
                    value[i] = (1+i) * u;
                    inputBufferData[i + u * NbOfCoeffs] = value;
                }
            }
        }

        [Test]
        public void RunTestPass2()
        {
             RunGameTest(new TestLambertPrefilteringSHPass2());
        }

        public static void Main()
        {
            using (var game = new TestLambertPrefilteringSHPass2(false))
            {
                game.Run();
            }
        }
    }
}