using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using iSpyApplication.Utilities;

namespace iSpyApplication
{
    public static class LocRm
    {
        private static Translations _translationsList;
        private static readonly Hashtable Res = new Hashtable();
        private static bool _inited;

        public static List<TranslationsTranslationSet> TranslationSets => TranslationsList.TranslationSet.ToList();

        private static Translations TranslationsList
        {
            get
            {
                if (_translationsList != null)
                    return _translationsList;
                var s = new XmlSerializer(typeof (Translations));
                string p = Program.AppDataPath + @"\XML\Translations.xml";
//#if DEBUG
//                p = Program.AppPath+ @"\XML\Translations.xml";
//#endif
                using (var fs = new FileStream(p, FileMode.Open))
                {
                    fs.Position = 0;
                    using (TextReader reader = new StreamReader(fs))
                    {
                        _translationsList = (Translations)s.Deserialize(reader);
                    }
                }

                return _translationsList;
            }
        }

        public static void Reset()
        {

            _translationsList = null;
            _inited = false;
        }

        public static string GetString(string identifier)
        {
            return GetString(identifier, MainForm.Conf.Language);
        }

        public static string GetString(string identifier, string languageCode)
        {
            if (!_inited)
            {
                Init();
            }

            // 1. Проверяем, что словарь существует
            if (Res == null)
            {
                Logger.LogError("Translation dictionary is null.");
                return identifier;
            }

            // 2. Приводим к нижнему регистру
            identifier = identifier.ToLower();
            languageCode = languageCode ?? MainForm.Conf.Language ?? "en";

            // 3. Формируем ключ
            string key = languageCode + "." + identifier;

            // 4. Пытаемся получить перевод
            if (Res.ContainsKey(key))
            {
                return Res[key].ToString();
            }

            // 5. Если перевода нет — логируем и возвращаем идентификатор
            Logger.LogError("No Translation for token " + identifier);

            // 6. Если язык не английский, добавляем ключ в словарь (кешируем отсутствие)
            if (languageCode != "en")
            {
                try
                {
                    Res.Add(key, identifier);
                }
                catch
                {
                    // Если ключ уже есть — игнорируем
                }
            }

            return identifier;
        }



        public static void SetString(Control ctrl, string identifier)
        {
            ctrl.Text = GetString(identifier);
        }

        public static void SetString(MenuItem ctrl, string identifier)
        {
            ctrl.Text = GetString(identifier);
        }
        public static void SetString(ToolStripMenuItem ctrl, string identifier)
        {
            ctrl.Text = GetString(identifier);
        }

        public static void SetString(ToolStripStatusLabel ctrl, string identifier)
        {
            ctrl.Text = GetString(identifier);
        }


        private static void Init()
        {
            if (MainForm.Conf.Language == "NotSet")
            {
                MainForm.Conf.Language = CultureInfo.CurrentCulture.Name.ToLower().Split('-')[0];
                var ts = TranslationSets.FirstOrDefault(p => p.CultureCode == MainForm.Conf.Language);
                if (ts == null)
                {
                    MainForm.Conf.Language = "en";
                }
            }

            Res.Clear();

            foreach (var ls in TranslationSets)
            {
                foreach (var t in ls.Translation)
                {
                    try
                    {
                        Res.Add((ls.CultureCode + "." + t.Token.ToLower()), t.Value);
                    }
                    catch(Exception ex)
                    {
                        //ignore duplicates
                    }
                }
            }
            _inited = true;
        }

    }
}