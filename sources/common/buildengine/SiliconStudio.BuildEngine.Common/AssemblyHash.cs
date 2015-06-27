﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.BuildEngine
{
    internal static class AssemblyHash
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("AssemblyHash");

        /// <summary>
        /// Computes the hash from an assembly based on its AssemblyFileVersion. Recurse to all assembly dependencies.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>A full hash of this assembly, including all its dependencies.</returns>
        public static string ComputeAssemblyHash(Assembly assembly)
        {
            string hash;
            lock (assemblyToHash)
            {
                if (!assemblyToHash.TryGetValue(assembly, out hash))
                {
                    var assemblies = new HashSet<Assembly>();
                    var text = new StringBuilder();
                    ComputeAssemblyHash(assembly, assemblies, text);
                    hash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(text.ToString())).ToString();
                    assemblyToHash.Add(assembly, hash);
                    Log.Info("Assembly Hash [{0}] => [{1}]", assembly.GetName().Name, hash);
                }
            }
            return hash;
        }

        private static readonly Dictionary<Assembly, string> assemblyToHash = new Dictionary<Assembly, string>();

        private static void ComputeAssemblyHash(Assembly assembly, HashSet<Assembly> assemblies, StringBuilder outputString)
        {
            if (assemblies.Contains(assembly))
                return;

            outputString.Append(assembly.FullName);

            var attribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (attribute != null)
            {
                outputString.Append(",").Append(attribute.Version);
                outputString.AppendLine();
            }

            assemblies.Add(assembly);
            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                // Avoid processing system assemblies
                // TODO: Scan what is actually in framework folders (and unify it with ProcessDataSerializerGlobalAttributes)
                if (referencedAssemblyName.Name == "mscorlib" || referencedAssemblyName.Name.StartsWith("System")
                    || referencedAssemblyName.FullName.Contains("PublicKeyToken=31bf3856ad364e35")) // Signed with Microsoft public key (likely part of system libraries)
                    continue;

                var assemblyRef = Assembly.Load(referencedAssemblyName);
                ComputeAssemblyHash(assemblyRef, assemblies, outputString);
            }
        }
    }
}