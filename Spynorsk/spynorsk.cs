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

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            // Add your language to the cycle/dropdown
            Localization instance = Localization.instance;
            FieldInfo langsField = typeof(Localization).GetField("m_languages", BindingFlags.NonPublic | BindingFlags.Instance);
            List<string> langsList = (List<string>)langsField.GetValue(instance);
            if (!langsList.Contains(customLang))
            {
                langsList.Add(customLang);
            }

            // Cache Bokmål translations for fallback
            var transField = typeof(Localization).GetField("m_translations", BindingFlags.NonPublic | BindingFlags.Instance);
            instance.SetLanguage(fallbackLang);
            FallbackTranslations = new Dictionary<string, string>((Dictionary<string, string>)transField.GetValue(instance));

            instance.SetLanguage(customLang);
        }

        [HarmonyPatch(typeof(Localization), "SetupLanguage")]
        public static class Localization_SetupLanguage_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Localization __instance, string language)
            {
                if (language != customLang) return true;

                // Access the internal dictionary
                var transField = typeof(Localization).GetField("m_translations", BindingFlags.NonPublic | BindingFlags.Instance);
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(langFile));

                var merge = FallbackTranslations;
                foreach (var kvp in translations)
                {
                    merge[kvp.Key] = kvp.Value;
                }

                // Reassign updated dictionary
                transField.SetValue(__instance, merge);
                Main.logger.LogInfo($"Language {customLang} loaded with {translations.Count} translations.");

                return false;
            }
        }

        [HarmonyPatch(typeof(Localization), "GetLanguages")]
        public static class GetLanguagesPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ref List<string> __result)
            {
                if (!__result.Contains(customLang))
                {
                    __result.Add(customLang);
                }
            }
        }

    }
}
