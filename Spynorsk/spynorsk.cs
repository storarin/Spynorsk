using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace Spynorsk
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        const string pluginGUID = "com.n00bworkstan.spynorsk";
        const string pluginName = "Spynorsk Mordbok";
        const string pluginVersion = "1.0.0";

        private readonly Harmony HarmonyInstance = new Harmony(pluginGUID);
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        static readonly string langDir = Path.Combine(Paths.PluginPath, "lang");
        static readonly Dictionary<string, Language> customLangs = new Dictionary<string, Language>();

        public struct Language
        {
            public string Name;
            public string Fallback;
            public Dictionary<string, string> Translations;
        }

        public void Awake()
        {
            Main.logger.LogInfo(@"
                              --*#**+********+++
                          //******#*#*******+*++*++
                        //*#*****#***************+***
                      /**++*****#***#******##***+*+*+*
                     /*+*************####*****+**+++***
                    |=++*++*++++++*+**********++**+****+
                    +===+++**++++**+*++*******+****+++++=
                   =-=+=+++=+++++**+***+**###*#####****#+
                   |-===+-=+-=====:.::==+***#*##*##*+****=
                   |::....:-=---::.....::=++#######*+##**+
                    ......:..==----.::---\\+*#######+*#***
                    ....-\-:-##***+=/-----*-**##*#**=++#+*=
                   -:::::===*#***#*++-=+++********+**+*/-|*
                   |=---==+=*#*##*###****####+++==+*=++*||*
                   |---===/+####*#*\*##*#*****++=++*||:+*++
                   :::::./::-==-..*+:=++***++=-=-+*+##*//-+
                   ::....=.....:=+***---++++=-===****#/+=
                   :-....:.....--====+\-=++*=-=|+||-/+++
                   -:.........--+---\\++=***+=++*||=+=
                   -::.::..::..:::-++*++****++=+**+\\+
                   -:-:..::..::--+++=++*++=++=+***#\\\
                    -:::....-::-=-++==-==++==\\####*\\:
                    ---:::.:....:.:--:::::-||\|*##*|\|::
                     =/:/-::::.:-::////|/||+*+****\.::::::
                      ---/:/-/-://|*//|||*#*##/\\:::::.::-::-
                   :.:::-+-/+/\/|||\||+|##%///:::::::::::::::-:
                 .::.:::::--::-:/:\-\\\#%//::.:-::::::::-:--::---=
              :.:::::::::::::::::::...:\:::-::---:-:::---:---:------=
           =:.::--:-::..:::::::::::..:..:::::----::.::-:-:---:-----==-=
");

            if (!Directory.Exists(langDir))
            {
                Directory.CreateDirectory(langDir);
                logger.LogWarning($"Lang directory not found. Created new directory at: {langDir}");
            }

            // Load all language files
            foreach (string file in Directory.GetFiles(langDir, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    Language lang = JsonConvert.DeserializeObject<Language>(json);
                    if (string.IsNullOrEmpty(lang.Name))
                    {
                        logger.LogWarning($"Skipped file '{Path.GetFileName(file)}' (missing Name).");
                        continue;
                    }

                    if (lang.Translations == null)
                        lang.Translations = new Dictionary<string, string>();

                    customLangs[lang.Name] = lang;
                    logger.LogInfo($"Loaded language definition: '{lang.Name}'");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to load {file}: {ex.Message}");
                }
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            Localization instance = Localization.instance;
            FieldInfo langsField = typeof(Localization).GetField("m_languages", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> langsList = (List<string>)langsField.GetValue(instance);

            foreach (var lang in customLangs.Keys)
            {
                if (!langsList.Contains(lang))
                    langsList.Add(lang);
            }

            logger.LogInfo($"Registered {customLangs.Count} custom languages.");
        }

        [HarmonyPatch(typeof(Localization), "SetupLanguage")]
        public static class Localization_SetupLanguage_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Localization __instance, string language, ref bool __result)
            {
                Language custom;
                if (!customLangs.TryGetValue(language, out custom))
                    return true; // Proceed with original method

                FieldInfo transField = typeof(Localization).GetField("m_translations", BindingFlags.NonPublic | BindingFlags.Instance);
                Dictionary<string, string> translations = new Dictionary<string, string>();

                // Load fallback translations
                if (!string.IsNullOrEmpty(custom.Fallback))
                {
                    __instance.SetupLanguage(custom.Fallback);
                    translations = (Dictionary<string, string>)transField.GetValue(__instance);
                }

                // Merge with fallback translations
                foreach (var kvp in custom.Translations)
                {
                    translations[kvp.Key] = kvp.Value;
                }

                // Reassign updated dictionary
                transField.SetValue(__instance, translations);
                Main.logger.LogInfo($"Loaded language '{language}' with {custom.Translations.Count} translations{(!string.IsNullOrEmpty(custom.Fallback) ? $" and '{custom.Fallback}' as fallback" : "")}.");

                __result = true;
                return false;
            }
        }

    }
}
