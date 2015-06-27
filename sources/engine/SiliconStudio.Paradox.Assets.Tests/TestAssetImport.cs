﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.IO;
using SiliconStudio.Paradox.Rendering.Materials;
using SiliconStudio.Paradox.Rendering.Materials.Processor.Visitors;
using SiliconStudio.Paradox.Assets.Model;
using SiliconStudio.Paradox.Assets.Textures;

namespace SiliconStudio.Paradox.Assets.Tests
{
/*
    TODO: TO REWRITE WITH new AssetImportSession

    [TestFixture]
    public class TestAssetImport
    {
        public const string DirectoryTestBase = @"data\SiliconStudio.Paradox.Assets.Tests\";

        [TestFixtureSetUp]
        public void Initialize()
        {
        }

        [Test]
        public void TestImportTexture()
        {
            var projectDir = Path.Combine(DirectoryTestBase, "TestImportTexture");
            DeleteDirectory(projectDir);

            var project = new Project { ProjectPath = projectDir + "/test.pdxpkg" };
            var session = new ProjectSession(project);
            Import(project, "texture", Path.Combine(DirectoryTestBase, "Logo.png"));

            // Save the project
            var result = session.Save();
            Assert.IsFalse(result.HasErrors);

            Assert.True(File.Exists(projectDir + "/texture/logo.pdxtex"));

            var textureAsset = AssetSerializer.Load<TextureAsset>(projectDir + "/texture/logo.pdxtex");

            Assert.AreEqual("../../Logo.png", textureAsset.Source.FullPath);

            // Cleanup before exit
            DeleteDirectory(projectDir);
        }

        [Test]
        public void TestImportModelWithTextures()
        {
            var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithTextures");
            DeleteDirectory(projectDir);

            var project = new Project { ProjectPath = projectDir + "/test.pdxpkg" };
            var session = new ProjectSession(project);
            Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));

            var result = session.Save();
            Assert.IsFalse(result.HasErrors);

            Assert.True(File.Exists(projectDir + "/model/factory_entity.pdxentity"));

            var modelAsset = AssetSerializer.Load<EntityAsset>(projectDir + "/model/factory_entity.pdxentity");

            Assert.AreEqual("factory_model", modelAsset.Data.Name);

            var textureAsset = AssetSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.pdxtex");

            Assert.AreEqual("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

            // Cleanup before exit
            DeleteDirectory(projectDir);
        }

        [Test]
        public void TestImportModelWithMaterialAndTextures()
        {
            var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithMaterialAndTextures");
            DeleteDirectory(projectDir);

            var project = new Project();
            var session = new ProjectSession(project);
            project.ProjectPath = projectDir + "/test.pdxpkg";
            Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));
            session.Save();

            // 2 materials, 1 model, 1 entity, 1 texture
            Assert.AreEqual(5, project.Assets.Count);

            Assert.True(File.Exists(projectDir + "/model/factory_material_blinn1.pdxmat"));
            Assert.True(File.Exists(projectDir + "/model/factory_material_blinn2.pdxmat"));
            Assert.True(File.Exists(projectDir + "/model/factory.pdxm3d"));

            var textureAsset = AssetSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.pdxtex");
            
            Assert.AreEqual("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

            var materialBlinn1 = AssetSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn1.pdxmat");
            var textureVisitor = new MaterialTextureVisitor(materialBlinn1.Material);
            var allTexturesBlinn1 = textureVisitor.GetAllTextureValues();
            Assert.AreEqual(1, allTexturesBlinn1.Count);
            foreach (var texture in allTexturesBlinn1)
                Assert.AreNotEqual(texture.Texture.Id, textureAsset.Id);

            var materialBlinn2 = AssetSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn2.pdxmat");
            textureVisitor = new MaterialTextureVisitor(materialBlinn2.Material);
            var allTexturesBlinn2 = textureVisitor.GetAllTextureValues();
            Assert.AreEqual(1, allTexturesBlinn2.Count);
            foreach (var texture in allTexturesBlinn2)
                Assert.AreEqual(texture.Texture.Id, textureAsset.Id);

            var model = AssetSerializer.Load<ModelAsset>(projectDir + "/model/factory.pdxm3d");


            // Cleanup before exit
            DeleteDirectory(projectDir);
        }

        [Test]
        public void TestImportModelWithMaterialAndTextures2()
        {
            var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithMaterialAndTextures");
            DeleteDirectory(projectDir);

            var project = new Project();
            var session = new ProjectSession(project);
            project.ProjectPath = projectDir + "/test.pdxpkg";
            Import(project, "model", Path.Combine(DirectoryTestBase, "knight.fbx"));
            session.Save();

            Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_KINGHT.pdxmat"));
            Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_KINGHT_iron.pdxmat"));
            Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_SWORD1.pdxmat"));
            
            // Cleanup before exit
            DeleteDirectory(projectDir);

            project = new Project();
            session = new ProjectSession(project);
            project.ProjectPath = projectDir + "/test.pdxpkg";
            Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));
            session.Save();

            Assert.True(File.Exists(projectDir + "/model/factory_material_blinn1.pdxmat"));
            Assert.True(File.Exists(projectDir + "/model/factory_material_blinn2.pdxmat"));
            Assert.True(File.Exists(projectDir + "/model/factory.pdxm3d"));

            var textureAsset = AssetSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.pdxtex");
            
            Assert.AreEqual("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

            var materialBlinn1 = AssetSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn1.pdxmat");
            var textureVisitor = new MaterialTextureVisitor(materialBlinn1.Material);
            foreach (var texture in textureVisitor.GetAllTextureValues())
                Assert.AreNotEqual(texture.Texture.Id, textureAsset.Id);

            var materialBlinn2 = AssetSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn2.pdxmat");
            textureVisitor = new MaterialTextureVisitor(materialBlinn2.Material);
            foreach (var texture in textureVisitor.GetAllTextureValues())
                Assert.AreEqual(texture.Texture.Id, textureAsset.Id);

            var model = AssetSerializer.Load<ModelAsset>(projectDir + "/model/factory.pdxm3d");


            // Cleanup before exit
            DeleteDirectory(projectDir);
        }
        
        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Imports a raw asset from the specified asset file path using importers registered in <see cref="AssetImporterRegistry" />.
        /// </summary>
        /// <param name="projectRelativeDirectory">The directory relative to the project where this asset should be imported.</param>
        /// <param name="filePathToRawAsset">The file path to raw asset.</param>
        /// <exception cref="System.ArgumentNullException">filePathToRawAsset</exception>
        /// <exception cref="AssetException">Unable to find a registered importer for the specified file extension [{0}]</exception>
        private static void Import(Project project, UDirectory projectRelativeDirectory, string filePathToRawAsset)
        {
            if (projectRelativeDirectory == null) throw new ArgumentNullException("projectRelativeDirectory");
            if (filePathToRawAsset == null) throw new ArgumentNullException("filePathToRawAsset");

            if (projectRelativeDirectory.IsAbsolute)
            {
                throw new ArgumentException("Project directory must be relative to project and not absolute", "projectRelativeDirectory");
            }

            // Normalize input path
            filePathToRawAsset = FileUtility.GetAbsolutePath(filePathToRawAsset);
            if (!File.Exists(filePathToRawAsset))
            {
                throw new FileNotFoundException("Unable to find file [{0}]".ToFormat(filePathToRawAsset), filePathToRawAsset);
            }

            // Check that an importer was found
            IAssetImporter importer = AssetRegistry.FindImporterByExtension(Path.GetExtension(filePathToRawAsset)).FirstOrDefault();
            if (importer == null)
            {
                throw new AssetException("Unable to find a registered importer for the specified file extension [{0}]", filePathToRawAsset);
            }

            List<AssetItem> newAssets = importer.Import(filePathToRawAsset, importer.GetDefaultParameters(false)).ToList();

            // Remove any asset which already exists
            var newAssetLocations = new HashSet<UFile>(newAssets.Select(x => x.Location));
            project.Assets.RemoveWhere(x => newAssetLocations.Contains(x.Location));

            // Add imported assets to this project
            foreach (var assetReference in newAssets)
            {
                project.Assets.Add(assetReference);
            }
        }
    }
 */
}