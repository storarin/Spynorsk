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

        private static Dictionary<string, string> FallbackTranslations;

        const string customLang = "Spynorsk";
        const string fallbackLang = "Norwegian";
        static readonly string langFile = Path.Combine(Paths.PluginPath, "lang", "spynorsk.json");

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

            Localization instance = Localization.instance;

            // Cache translations for fallback
            var transField = typeof(Localization).GetField("m_translations", BindingFlags.NonPublic | BindingFlags.Instance);
            instance.SetLanguage(fallbackLang);
            FallbackTranslations = new Dictionary<string, string>((Dictionary<string, string>)transField.GetValue(instance));

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            // Add custom language to the list of languages
            FieldInfo langsField = typeof(Localization).GetField("m_languages", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> langsList = (List<string>)langsField.GetValue(instance);
            if (!langsList.Contains(customLang))
            {
                langsList.Add(customLang);
            }

            instance.SetLanguage(customLang);
        }

        [HarmonyPatch(typeof(Localization), "SetupLanguage")]
        public static class Localization_SetupLanguage_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Localization __instance, string language, ref bool __result)
            {
                if (language != customLang) return true;

                // Access the internal dictionary
                var transField = typeof(Localization).GetField("m_translations", BindingFlags.NonPublic | BindingFlags.Instance);
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(langFile));
                int count = translations.Count;

                foreach (var kvp in FallbackTranslations)
                {
                    if (!translations.ContainsKey(kvp.Key))
                    {
                        translations[kvp.Key] = kvp.Value;
                    }
                }

                // Reassign updated dictionary
                transField.SetValue(__instance, translations);
                Main.logger.LogInfo($"Language {customLang} loaded with {count} translations.");
                __result = true;
                return false;
            }
        }

    }
}
