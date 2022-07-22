using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using OAData.Adapters;

namespace OAData
{
    public class OADB
    {
        public static CassInfo[] cassettes = null;
        public static FogInfo[] fogs = null;
        public static DAdapter adapter = null;

        public static string look = "";

        private static string path;
        private static XElement _xconfig = null;
        public static XElement XConfig { get { return _xconfig; } }

        public static bool directreload = true; 
        public static bool initiated = false;
        public static bool nodatabase = true;
        public static void Init(string pth)
        {
            path = pth;
            Init();
            initiated = true;
        }
        public static string configfilename = "config.xml";
        public static Dictionary<string, string> toNormalForm = null;
        public static void Init()
        {
            // Создание словаря если есть файл zaliznyak_shortform.zip
            if (File.Exists(path + "zaliznyak_shortform.zip"))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(path + "zaliznyak_shortform.zip", path);
                var reader = new StreamReader(path + "zaliznyak_shortform.txt");
                toNormalForm = new Dictionary<string, string>();
                string line = null;
                string normal = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(' ');
                    normal = parts[0];
                    foreach (string w in parts)
                    {
                        if (!toNormalForm.ContainsKey(w)) toNormalForm.Add(w, normal);
                    }
                }
                reader.Close();
                File.Delete(path + "zaliznyak_shortform.txt");
            }

            XElement xconfig = XElement.Load(path + configfilename);
            _xconfig = xconfig;
            // Кассеты перечислены через элементы LoadCassette. Имена кассет в файловой системе должны сравниваться по lower case
            cassettes = xconfig.Elements("LoadCassette")
                .Select(lc =>
                {
                    string cassPath = lc.Value;
                    XAttribute write_att = lc.Attribute("write");
                    string name = cassPath.Split('/', '\\').Last();
                    return new CassInfo()
                    {
                        name = name,
                        path = cassPath,
                        writable = (write_att != null && (write_att.Value == "yes" || write_att.Value == "true"))
                    };
                })
                .ToArray();

            // PrepareFogs(xconfig); -- перенесен в Load

            // Подключение к базе данных, если задано
            string connectionstring = xconfig.Element("database")?.Attribute("connectionstring")?.Value;
            if (connectionstring != null)
            {
                string pre = connectionstring.Substring(0, connectionstring.IndexOf(':'));
                //if (pre == "trs")
                //{
                //    adapter = new TripleRecordStoreAdapter();
                //}
                //else 
                if (pre == "xml")
                {
                    adapter = new XmlDbAdapter();
                }
                // else if (pre == "om")
                // {
                //     adapter = new OmAdapter();
                // }
                else if (pre == "uni")
                {
                    adapter = new UniAdapter();
                }
                adapter.Init(connectionstring);
                PrepareFogs(XConfig);

                if (!adapter.nodatabase) nodatabase = false;

                if (pre == "trs" && (directreload || nodatabase)) Load();
                else if (pre == "xml") Load();
                else if (pre == "om" && (directreload || nodatabase)) Load();
                else if (pre == "uni" && (directreload || nodatabase)) Load();

                // Логфайл элементов Put()
                //putlogfilename = connectionstring.Substring(connectionstring.IndexOf(':') + 1) + "logfile_put.txt";
                putlogfilename = path + "logfile_put.txt";
            }
        }

        private static void PrepareFogs(XElement xconfig)
        {
            // Формирую список фог-документов
            List<FogInfo> fogs_list = new List<FogInfo>();
            // Прямое попадание в список фогов из строчек конфигуратора
            foreach (var lf in xconfig.Elements("LoadFog"))
            {
                string fogname = lf.Value;
                int lastpoint = fogname.LastIndexOf('.');
                if (lastpoint == -1) throw new Exception("Err in fog file name construction");
                string ext = fogname.Substring(lastpoint).ToLower();
                bool writable = (lf.Attribute("writable")?.Value == "true" || lf.Attribute("write")?.Value == "yes") ?
                    true : false;
                var atts = ReadFogAttributes(fogname);
                fogs_list.Add(new FogInfo()
                {
                    vid = ext,
                    pth = fogname,
                    writable = writable && atts.prefix != null && atts.counter != null,
                    owner = atts.owner,
                    prefix = atts.prefix,
                    counter = atts.counter == null ? -1 : Int32.Parse(atts.counter)
                });

            }
            // Сбор фогов из кассет
            for (int i = 0; i < cassettes.Length; i++)
            {
                // В каждой кассете есть фог-элемент meta/имякассеты_current.fog, в нем есть владелец и может быть запрет на запись в виде
                // отсутствия атрибутов prefix или counter. Также там есть uri кассеты, надо проверить.
                CassInfo cass = cassettes[i];
                string pth = cass.path + "/meta/" + cass.name + "_current.fog";
                var atts = ReadFogAttributes(pth);
                // запишем владельца, уточним признак записи
                cass.owner = atts.owner;
                if (atts.prefix == null || atts.counter == null) cass.writable = false;
                fogs_list.Add(new FogInfo()
                {
                    //cassette = cass,
                    pth = pth,
                    fogx = null,
                    owner = atts.owner,
                    writable = true //cass.writable,
                    //prefix = atts.prefix,
                    //counter = atts.counter
                });
                // А еще в кассете могут быть другие фог-документы. Они размещаются в originals
                IEnumerable<FileInfo> fgs = (new DirectoryInfo(cass.path + "/originals"))
                    .GetDirectories("????").SelectMany(di => di.GetFiles("*.fog"));
                // Быстро проглядим документы и поместим информацию в список фогов
                foreach (FileInfo fi in fgs)
                {
                    var attts = ReadFogAttributes(fi.FullName);

                    // запишем владельца, уточним признак записи
                    //cass.owner = attts.owner;
                    fogs_list.Add(new FogInfo()
                    {
                        //cassette = cass,
                        pth = fi.FullName,
                        fogx = null,
                        owner = attts.owner,
                        //writable = cass.writable,
                        //prefix = attts.prefix,
                        //counter = attts.counter
                        writable = cass.writable && attts.prefix != null && attts.counter != null
                    }); ;
                }

            }
            // На выходе я определил, что будет массив
            fogs = fogs_list.ToArray();
            fogs_list = null;
        }

        public static void Load()
        {
            adapter.StartFillDb(null);
            adapter.FillDb(fogs, null);
            adapter.FinishFillDb(null);
        }
        public static void Reload()
        {
            Close();
            Init();
            Load();
        }
        private static string putlogfilename = null;
        public static void Close()
        {
            adapter.Close();
        }

        // Доступ к документным файлам по uri и параметрам
        public static string CassDirPath(string uri)
        {
            if (!uri.StartsWith("iiss://")) throw new Exception("Err: 22233");
            int pos = uri.IndexOf('@', 7);
            if (pos < 8) throw new Exception("Err: 22234");
            return cassettes.FirstOrDefault(c => c.name == uri.Substring(7, pos - 7))?.path;
        }
        public static string GetFilePath(string u, string s)
        {
            if (u == null) return null;
            u = System.Web.HttpUtility.UrlDecode(u);
            var cass_dir = OAData.OADB.CassDirPath(u);
            if (cass_dir == null) return null;
            string last10 = u.Substring(u.Length - 10);
            string subpath;
            string method = s;
            if (method == null) subpath = "/originals";
            if (method == "small") subpath = "/documents/small";
            else if (method == "medium") subpath = "/documents/medium";
            else subpath = "/documents/normal"; // (method == "n")
            string path = cass_dir + subpath + last10;
            return path;
        }


        // Доступ ка базе данных
        public static IEnumerable<XElement> SearchByName(string ss)
        {
            return adapter.SearchByName(ss);
        }
        public static IEnumerable<XElement> SearchByWords(string ss)
        {
            return adapter.SearchByWords(ss);
        }
        public static XElement GetItemByIdBasic(string id, bool addinverse)
        {
            var val = adapter.GetItemByIdBasic(id, addinverse);
            return val;
        }

        /// <summary>
        /// Делает портрет айтема, годный для простого преобразования в html-страницу или ее часть.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static XElement GetBasicPortrait(string id)
        {
            XElement tree = GetItemByIdBasic(id, true);
            return new XElement("record", new XAttribute(tree.Attribute("id")), new XAttribute(tree.Attribute("type")),
                tree.Elements().Where(el => el.Name == "field" || el.Name == "direct")
                .Select(el =>
                {
                    if (el.Name == "field") return new XElement(el);
                    string prop = el.Attribute("prop").Value;
                    string target = el.Element("record").Attribute("id").Value;
                    XElement tr = GetItemByIdBasic(target, false);
                    return new XElement("direct", new XAttribute("prop", prop),
                        new XElement("record",
                            new XAttribute(tr.Attribute("id")),
                            new XAttribute(tr.Attribute("type")),
                            tr.Elements()));
                }),
                null);
        }

        public static XElement GetItemById(string id, XElement format)
        {
            return adapter.GetItemById(id, format);
        }
        public static IEnumerable<XElement> GetAll()
        {
            return adapter.GetAll();
        }
        public static XElement UpdateItem(XElement item)
        {
            string id = item.Attribute(ONames.rdfabout)?.Value;
            if (id == null)
            {  // точно не апдэйт
                return PutItem(item);
            }
            else
            { // возможно update
                XElement old = adapter.GetItemByIdBasic(id, false);
                if (old == null) return PutItem(item);
                // добавляем старые, которых нет. Особенность в том, что старые - в запросном формате, новые - в базовом. 
                IEnumerable<XElement> adding = old.Elements()
                    .Select(oe =>
                    {
                        string prop_value = oe.Attribute("prop").Value;
                        string lang = oe.Attribute("{http://www.w3.org/XML/1998/namespace}lang")?.Value;
                        bool similar = item.Elements().Where(el => el.Name.NamespaceName+el.Name.LocalName == prop_value).Any(el =>
                        {
                            string olang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang")?.Value;
                            return (lang == null && olang == null ? true : lang == olang);
                        });
                        if (similar) return (XElement)null; // Если найден похожий, то не нужен старый
                        else
                        {
                            int pos = prop_value.LastIndexOf('/');
                            XName xn = XName.Get(prop_value.Substring(pos + 1), prop_value.Substring(0, pos+1));
                            return new XElement(xn, 
                                lang==null?null: new XAttribute("{http://www.w3.org/XML/1998/namespace}lang", lang),
                                oe.Value); // Добавляем старый
                        }
                    });
                XElement nitem = new XElement(item.Name, item.Attributes(), item.Elements(), adding);
                //// новые свойства. TODO: Языковые варианты опущены!
                //XElement nitem = new XElement(item);
                //string[] props = nitem.Elements().Select(el => el.Name.LocalName).ToArray();
                //nitem.Add(old.Elements()
                //.Select(el =>
                //{
                //    string prop = el.Attribute("prop").Value;
                //    int pos = prop.LastIndexOf('/');
                //    XName subel_name = XName.Get(prop.Substring(pos + 1), prop.Substring(0, pos + 1));
                //    if (props.Contains(prop.Substring(pos + 1))) return null;
                //    XElement subel = new XElement(subel_name);
                //    if (el.Name == "field") subel.Add(el.Value);
                //    else if (el.Name == "direct") subel.Add(new XAttribute(ONames.rdfresource, el.Element("record").Attribute("id").Value));
                //    return subel;
                //}));
                return PutItem(nitem);
            }
        }
        public static bool HasWritabeFogForUser(string user)
        {
            return fogs.Any(f => f.owner == user && f.writable);
        }
        public static XElement PutItem(XElement item)
        {
            //XElement result = null;
            string owner = item.Attribute("owner")?.Value;
            
            // Запись возможна только если есть код владельца
            if (owner == null) return new XElement("error", "no owner attribute");

            // Проверим и изменим отметку времени
            string mT = DateTime.Now.ToUniversalTime().ToString("u");
            XAttribute mT_att = item.Attribute("mT");
            if (mT_att == null) item.Add(new XAttribute("mT", mT));
            else  mT_att.Value = mT;
            
            
            // Ищем подходящий фог-документ
            FogInfo fi = fogs.FirstOrDefault(f => f.owner == owner && f.writable);
            
            // Если нет подходящего - запись не производится
            if (fi == null) return new XElement("error", "no writable fog for request");

            // Если фог не загружен, то загрузить его
            if (fi.fogx == null) fi.fogx = XElement.Load(fi.pth);


            // читаем или формируем идентификатор
            string id = item.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
            XElement element = null; // запись с пришедшим идентификатором
            if (id == null)
            {
                XAttribute counter_att = fi.fogx.Attribute("counter");
                int counter = Int32.Parse(counter_att.Value);
                id = fi.fogx.Attribute("prefix").Value + counter;
                // внедряем
                item.Add(new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id));
                counter_att.Value = "" + (counter + 1);
            }
            else
            {
                element = fi.fogx.Elements().FirstOrDefault(el => 
                    el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value == id);
            }

            // Изымаем из пришедшего элемента владельца и фиксируем его в фоге
            XAttribute owner_att = item.Attribute("owner");
            owner_att.Remove();
            if (element != null)
            {
                element.Remove();
            }

            // Очищаем запись от пустых полей
            XElement nitem = new XElement(item.Name, item.Attribute(ONames.rdfabout), item.Attribute("mT"), 
                item.Elements().Select(xprop =>
                {
                    XAttribute aresource = xprop.Attribute(ONames.rdfresource);
                    if (aresource == null)
                    {   // DatatypeProperty
                        if (string.IsNullOrEmpty(xprop.Value)) return null; // Глевное убирание!!!
                        return new XElement(xprop);
                    }
                    else
                    {   // ObjectProperty
                        return new XElement(xprop); //TODO: Возможно, надо убрать ссылки типа ""
                    }
                }),
                null);


            fi.fogx.Add(nitem);

            // Сохраняем файл
            fi.fogx.Save(fi.pth);

            // Сохраняем в базе данных
            adapter.PutItem(nitem);

            // Сохраняем в логе
            using (Stream log = File.Open(putlogfilename, FileMode.Append, FileAccess.Write))
            {
                TextWriter tw = new StreamWriter(log, System.Text.Encoding.UTF8);
                tw.WriteLine(nitem.ToString());
                tw.Close();
            }

            return new XElement(nitem);
        }


        private static (string owner, string prefix, string counter)  ReadFogAttributes(string pth)
        {
            // Нужно для чтиния в кодировке windows-1251. Нужен также Nuget System.Text.Encoding.CodePages
            var v = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(v);

            string owner = null;
            string prefix = null;
            string counter = null;
            XmlReaderSettings settings = new XmlReaderSettings();
            using (XmlReader reader = XmlReader.Create(pth, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "rdf:RDF") throw new Exception($"Err: Name={reader.Name}");
                        owner = reader.GetAttribute("owner");
                        prefix = reader.GetAttribute("prefix");
                        counter = reader.GetAttribute("counter");
                        break;
                    }
                }
            }
            //Console.WriteLine($"ReadFogAttributes({pth}) : {owner} {prefix} {counter} ");
            return (owner, prefix, counter);
        }
    }

}
