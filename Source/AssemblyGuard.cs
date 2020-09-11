using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Verse;

namespace PawnExtensions
{
    public static class AssemblyGuard
    {
        public static bool DisableOutdatedAssemblies(ModContentPack content, Assembly assembly = null)
        {
            if (assembly == null)
                assembly = typeof(AssemblyGuard).Assembly;

            if (!content.assemblies.loadedAssemblies.Contains(assembly))
                return false;

            Version selfVersion = assembly.GetName().Version;
            string guid = assembly.GetCustomAttribute<GuidAttribute>().Value;

            var modsWithOutdatedAssemblies = new Dictionary<ModContentPack, bool>();

            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                if (mod == content)
                    continue;

                var assemblyVersions = mod.assemblies.loadedAssemblies.Where(t => t.GetCustomAttribute<GuidAttribute>()?.Value == guid).Select(t => t.GetName().Version); ;

                if (!assemblyVersions.Any())
                    continue;

                if (assemblyVersions.Any(t => t > selfVersion))
                    return false;

                modsWithOutdatedAssemblies.Add(mod, assemblyVersions.Any(t => t < selfVersion));
            }

            foreach (var kvp in modsWithOutdatedAssemblies)
            {
                if (kvp.Value)
                    Log.Warning($"Mod '{kvp.Key.Name}' has an outdated version of assembly '{assembly.GetName().Name}' which means it won't be used, consider updating it!", true);
                else
                    Log.Message($"Mod '{kvp.Key.Name}' has a duplicate of assembly '{assembly.GetName().Name}'");

                kvp.Key.assemblies.loadedAssemblies.ForEach(t => Log.Message($"{t.FullName} {t.GetName().Version} {selfVersion}"));
                kvp.Key.assemblies.loadedAssemblies.RemoveAll(t => t.GetCustomAttribute<GuidAttribute>().Value == guid);
            }

            return true;
        }
    }
}
