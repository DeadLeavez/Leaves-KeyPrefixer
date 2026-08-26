using System.Collections.Generic;
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Services;

namespace Leaves_KeyPrefixer;

//RUn really late so we're after any mods that might add keys (WTT-Backport)
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 90000)]
public class KeyPrefixer(
    DatabaseService databaseService,
    ModHelper modHelper)
    : IOnLoad
{
    public Task OnLoad()
    {
        string modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        ModConfig config = modHelper.GetJsonDataFromFile<ModConfig>(modPath, "config/config.jsonc");

        if (!config.Enabled)
        {
            Leaves.Utils.Instance.printColor("[KeyPrefixer] Disabled in config - keys keep their vanilla names.", LogTextColor.Yellow);
            return Task.CompletedTask;
        }

        if (!config.PrefixName && !config.PrefixShortName)
        {
            Leaves.Utils.Instance.printColor("[KeyPrefixer] Both prefixName and prefixShortName are off - nothing to do.", LogTextColor.Yellow);
            return Task.CompletedTask;
        }

        Dictionary<string, string> keyMap = modHelper.GetJsonDataFromFile<Dictionary<string, string>>(modPath, "data/keymap.jsonc");

        // Collapse each key id to its prefix string.
        Dictionary<string, string> idToPrefix = new Dictionary<string, string>();
        HashSet<string> warnedMissing = new HashSet<string>();
        foreach (var pair in keyMap)
        {
            string id = pair.Key;
            string mapLabel = pair.Value;

            if (!config.MapPrefixes.TryGetValue(mapLabel, out var prefix))
            {
                if (warnedMissing.Add(mapLabel))
                {
                    Leaves.Utils.Instance.printColor($"[KeyPrefixer] Map label \"{mapLabel}\" from keymap.jsonc has no entry in mapPrefixes - those keys are left unprefixed.", LogTextColor.Yellow);
                }
                continue;
            }

            if (string.IsNullOrEmpty(prefix))
            {
                continue;
            }

            idToPrefix[id] = prefix;
        }

        if (idToPrefix.Count == 0)
        {
            Leaves.Utils.Instance.printColor("[KeyPrefixer] No keys resolved to a non-empty prefix - nothing to do.", LogTextColor.Yellow);
            return Task.CompletedTask;
        }

        bool prefixName = config.PrefixName;
        bool prefixShortName = config.PrefixShortName;

        //I hate closures.
        Dictionary<string, string>? Transform(Dictionary<string, string>? locale)
        {
            if (locale == null)
            {
                return locale;
            }

            foreach (var pair in idToPrefix)
            {
                string id = pair.Key;
                string prefix = pair.Value;

                if (prefixName)
                {
                    ApplyPrefix(locale, id + " Name", prefix);
                }
                if (prefixShortName)
                {
                    ApplyPrefix(locale, id + " ShortName", prefix);
                }
            }

            return locale;
        }

        // Register on every language
        int languageCount = 0;
        foreach (var language in databaseService.GetLocales().Global)
        {
            language.Value.AddTransformer(Transform);
            languageCount++;
        }

        Leaves.Utils.Instance.printColor($"[KeyPrefixer] Armed - prefixing {idToPrefix.Count} keys across {languageCount} languages.", LogTextColor.Cyan);
        return Task.CompletedTask;
    }

    private static void ApplyPrefix(Dictionary<string, string> locale, string localeKey, string prefix)
    {
        if (!locale.TryGetValue(localeKey, out var current))
        {
            return;
        }
        if (current == null || current.StartsWith(prefix))
        {
            return;
        }
        locale[localeKey] = prefix + current;
    }
}
