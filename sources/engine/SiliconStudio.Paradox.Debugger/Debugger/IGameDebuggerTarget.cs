﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.ServiceModel;
using SiliconStudio.Paradox.Engine;

namespace SiliconStudio.Paradox.Debugger.Target
{
    /// <summary>
    /// Controls a game execution host, that can load and unload assemblies, run games and update assets.
    /// </summary>
    [ServiceContract]
    public interface IGameDebuggerTarget
    {
        #region Target
        [OperationContract]
        void Exit();
        #endregion

        #region Assembly
        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns></returns>
        [OperationContract]
        DebugAssembly AssemblyLoad(string assemblyPath);

        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="peData">The PE data.</param>
        /// <param name="pdbData">The PDB data.</param>
        /// <returns></returns>
        [OperationContract]
        DebugAssembly AssemblyLoadRaw(byte[] peData, byte[] pdbData);

        /// <summary>
        /// Unloads a previously loaded assembly.
        /// </summary>
        /// <param name="debugAssembly">The debug assembly.</param>
        /// <returns></returns>
        [OperationContract]
        bool AssemblyUnload(DebugAssembly debugAssembly);
        #endregion

        #region Game
        /// <summary>
        /// Enumerates the game types available in the currently loaded assemblies.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        List<string> GameEnumerateTypeNames();

        /// <summary>
        /// Instantiates and launches the specified game, found using its type name.
        /// </summary>
        /// <param name="gameTypeName">Name of the game type.</param>
        [OperationContract]
        void GameLaunch(string gameTypeName);

        /// <summary>
        /// Stops the current game, using <see cref="Game.Exit"/>.
        /// </summary>
        [OperationContract]
        void GameStop();
        #endregion

        #region Assets
        #endregion
    }
}