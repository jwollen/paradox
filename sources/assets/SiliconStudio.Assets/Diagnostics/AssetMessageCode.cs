// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
namespace SiliconStudio.Assets.Diagnostics
{
    /// <summary>
    /// A message code used by <see cref="AssetLogMessage"/> to identify an error/warning.
    /// </summary>
    /// Note that internally AssetMessageStrings.resx should contain an associated error message to this enum.
    public enum AssetMessageCode
    {
        /// <summary>
        /// A raw asset was not found
        /// </summary>
        RawAssetNotFound,

        /// <summary>
        /// The asset not found
        /// </summary>
        AssetNotFound,

        /// <summary>
        /// The asset reference has been changed for a particular location
        /// </summary>
        AssetReferenceChanged,

        /// <summary>
        /// The asset loading failed
        /// </summary>
        AssetLoadingFailed,

        /// <summary>
        /// The asset cannot be deleted
        /// </summary>
        AssetCannotDelete,

        /// <summary>
        /// The asset cannot be saved
        /// </summary>
        AssetCannotSave,

        /// <summary>
        /// The package not found
        /// </summary>
        PackageNotFound,

        /// <summary>
        /// The package filepath is not set for saving
        /// </summary>
        PackageFilePathNotSet,

        /// <summary>
        /// The package not found
        /// </summary>
        PackageLocationChanged,

        /// <summary>
        /// The package cannot be saved
        /// </summary>
        PackageCannotSave,

        /// <summary>
        /// The package dependency is modified
        /// </summary>
        PackageDependencyModified,

        /// <summary>
        /// The package build profile cannot be null
        /// </summary>
        BuildProfileCannotBeNull,

        /// <summary>
        /// The package build profile should not have a File extension null
        /// </summary>
        BuildProfileFileExtensionCannotBeNull,

        /// <summary>
        /// Asset contains invalid circular references
        /// </summary>
        InvalidCircularReferences,

        /// <summary>
        /// The base not found
        /// </summary>
        BaseNotFound,

        /// <summary>
        /// The base was changed
        /// </summary>
        BaseChanged,

        /// <summary>
        /// The base is not the same type as the current asset.
        /// </summary>
        BaseInvalidType,

        /// <summary>
        /// The asset has been successfully compiled.
        /// </summary>
        CompilationSucceeded,

        /// <summary>
        /// The asset compilation failed.
        /// </summary>
        CompilationFailed,

        /// <summary>
        /// The asset compilation has been cancelled.
        /// </summary>
        CompilationCancelled,

        /// <summary>
        /// The asset has not been compiled because it is already up-to-date.
        /// </summary>
        AssetUpToDate,

        /// <summary>
        /// The asset has not been compiled because its prerequisites failed to compile.
        /// </summary>
        PrerequisiteFailed,

        /// <summary>
        /// An unexpected internal error occured.
        /// </summary>
        InternalCompilerError,

        /// <summary>
        /// A fatal error that caused the asset compilation to fail.
        /// </summary>
        CompilationFatal,

        /// <summary>
        /// A message log happened inside the asset compiler.
        /// </summary>
        CompilationMessage,

        /// <summary>
        /// An error that caused the asset compilation to fail.
        /// </summary>
        CompilationError,

        /// <summary>
        /// A warning that occured in the asset compilation.
        /// </summary>
        CompilationWarning,

        /// <summary>
        /// A default scene was not found in the package.
        /// </summary>
        DefaultSceneNotFound,
    }
}