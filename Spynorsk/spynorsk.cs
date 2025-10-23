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
        const string customLangID = "language_spynorsk";
        const string fallbackLang = "Norwegian";
        static readonly string langFile = Path.Combine(Paths.PluginPath, "lang", "spynorsk.json");

        public void Awake()
        {
            Main.logger.LogInfo(@".....     .     _                                          ..                             .x+=:.
  .d88888Neu. 'L   u                                        :**888H: `: .xH""""                z`    ^%
  F""""""""*8888888F  88Nu.   u.                 .u    .       X   `8888k XX888                     .   <k               u.    u.
 *      `""*88*""  '88888.o888c       u      .d88B :@8c     '8hx  48888 ?8888          u        .@8Ned8""      .u     x@88k u@88c.
  -....    ue=:.  ^8888  8888    us888u.  =""8888f8888r    '8888 '8888 `8888       us888u.   .@^%8888""    ud8888.  ^""8888""""8888""
         :88N  `   8888  8888 .@88 ""8888""   4888>'88""      %888>'8888  8888    .@88 ""8888"" x88:  `)8b. :888'8888.   8888  888R
         9888L     8888  8888 9888  9888    4888> '          ""8 '888""  8888    9888  9888  8888N=*8888 d888 '88%""   8888  888R
  uzu.   `8888L    8888  8888 9888  9888    4888>           .-` X*""    8888    9888  9888   %8""    R88 8888.+""      8888  888R
,""""888i   ?8888   .8888b.888P 9888  9888   .d888L .+          .xhx.    8888    9888  9888    @8Wou 9%  8888L        8888  888R
4  9888L   %888>   ^Y8888*""""  9888  9888   ^""8888*""         .H88888h.~`8888.>  9888  9888  .888888P`   '8888c. .+  ""*88*"" 8888""
'  '8888   '88%      `Y""      ""888*""""888""     ""Y""          .~  `%88!` '888*~   ""888*""""888"" `   ^""F      ""88888%      """"   'Y""
     ""*8Nu.z*""                 ^Y""   ^Y'                         `""     """"      ^Y""   ^Y'                 ""YP'
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

            Main.logger.LogInfo("No har eg putta nynorsk i spelet ditt, gitt!");
        }
        private static readonly Dictionary<string, string> CustomTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(langFile));

        // Patch Translate to inject custom translations
        [HarmonyPatch(typeof(Localization), "Translate")]
        public static class TranslatePatch
        {
            [HarmonyPrefix]
            private static bool Prefix(ref string __result, string word)
            {
                if (Localization.instance.GetSelectedLanguage() == customLang || word == customLangID)
                {
                    if (Main.CustomTranslations.TryGetValue(word, out var custom))
                    {
                        __result = custom;
                        return false;
                    }
                    if (FallbackTranslations != null && FallbackTranslations.TryGetValue(word, out var fallback))
                    {
                        __result = fallback;
                        return false;
                    }
                }
                return true;
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
