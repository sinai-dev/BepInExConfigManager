using System.Collections.Generic;
using Mono.Cecil;
using System;
using BepInEx;
using BepInEx.Configuration;
using System.Linq;
using HarmonyLib;

#if CPP
using BepInEx.Preloader.Core.Patching;

[PatcherPluginInfo(Patcher.GUID, "BepInExConfigManager.Patcher", "1.3.0")]
class ConfigManagerPatcher : BasePatcher
{
    public override void Initialize()
    {
        Patcher.Init();
    }
}
#endif

public static class Patcher
{
    internal const string GUID = "com.sinai.bepinexconfigmanager.patcher";

    public static List<CachedConfigFile> ConfigFiles { get; } = new();

    public static Action<CachedConfigFile> ConfigFileCreated;

#if MONO
    public static IEnumerable<string> TargetDLLs { get; } = Enumerable.Empty<string>();
    public static void Patch(AssemblyDefinition _) { }

static Patcher()
    {
        Init();
    }
#endif

    internal static void Init()
    {
        new Harmony(GUID).PatchAll();
    }

    // Patch the ConfigFile ctor instead of searching for plugins

    [HarmonyPatch(typeof(ConfigFile), MethodType.Constructor, new Type[] { typeof(string), typeof(bool), typeof(BepInPlugin) })]
    internal static class ConfigFile_ctor
    {
        internal static void Postfix(ConfigFile __instance, BepInPlugin ownerMetadata)
        {
            CachedConfigFile cached = new(__instance, ownerMetadata);

            ConfigFiles.Add(cached);

            ConfigFileCreated?.Invoke(cached);
        }
    }
}

public class CachedConfigFile
{
    public readonly ConfigFile configFile;
    public readonly BepInPlugin metadata;

    public CachedConfigFile(ConfigFile configFile, BepInPlugin metadata)
    {
        this.configFile = configFile;
        this.metadata = metadata;
    }
}