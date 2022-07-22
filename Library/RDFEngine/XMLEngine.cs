using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RDFEngine
{
    public class XMLEngine : IEngine
    {
        // Основным объектом будет XML-представление RDF-данных
        private XElement rdf;


        // Констрируется пустая база данных
        public XMLEngine()
        {
            Clear();
        }

        // Пустая база данных состоит из элемента rdf:RDF и нужных для удобного вывода пространств имен. Для начала, используется только стандарт
        public void Clear()
        {
            rdf = XElement.Parse(
@"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
</rdf:RDF>");
        }

        // Загрузка части базы данных, таких загрузок может быть несколько
        public void Load(IEnumerable<XElement> records)
        {
            rdf.Add(records);
        }

        // Дополнительные структуры к rdf. Словарь элементов и словарь ссылочных элементов
        private Dictionary<string, XElement> recordsById;
        private Dictionary<string, XElement[]> subelementsByResource;


        public void Build()
        {
            // Предполагается, что элементы rdf загружены, базу данных надо "доделать" и сформировать дополнительные структуры
            recordsById = new Dictionary<string, XElement>();
            // Для размещения ссылок на ресурсы использую более удобное решение, а потом преобразую в subelementsByResource
            Dictionary<string, List<XElement>> subelements = new Dictionary<string, List<XElement>>();
            
            // Сканирую записи и "разбрасываю" ссылки по словарям
            foreach (XElement rec in rdf.Elements())
            {
                recordsById.Add(rec.Attribute(IEngine.rdfabout).Value, rec);
                foreach (XElement sub in rec.Elements())
                {
                    string resource = sub.Attribute(IEngine.rdfresource)?.Value;
                    if (resource == null) continue;
                    if (subelements.ContainsKey(resource))
                    {
                        subelements[resource].Add(sub);
                    }
                    else
                    {
                        subelements.Add(resource, new List<XElement>(new XElement[] { sub }));
                    }
                }
            }
            // Теперь для экономии перекину subelements в subelementsByResource
            subelementsByResource = subelements.Select(x => new { x.Key, x.Value })
                .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        // Сначала сделаем преобразователь записи в промежуточное представление
        private XElement ConvertToIntermediate(XElement rec, string unused_direct_prop)
        {
            return new XElement("record",
                new XAttribute("id", rec.Attribute(IEngine.rdfabout).Value),
                new XAttribute("type", rec.Name.NamespaceName + rec.Name.LocalName),
                rec.Elements()
                    .Select(el =>
                    {
                        string prop = el.Name.NamespaceName + el.Name.LocalName;
                        XAttribute resource = el.Attribute(IEngine.rdfresource);
                        if (resource != null)
                        {
                            if (prop == unused_direct_prop) return null;
                            return new XElement("direct", new XAttribute("prop", prop),
                                new XElement("record", new XAttribute("id", resource.Value)));
                        }
                        else
                        {
                            XAttribute xlang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                            return new XElement("field", new XAttribute("prop", prop), 
                                xlang == null ? null : new XAttribute(xlang),
                                el.Value);
                        }
                    }));
        }

        // Поиск айтемов по образцу имени (сравнение в нижнем регистре начала значения поля name
        public IEnumerable<XElement> Search(string sample)
        {
            sample = sample.ToLower(); // Сравнивать будем в нижнем регистре
            var query = rdf.Elements()
                .Where(r => r.Elements("name").Any(f => f.Value.StartsWith(sample)))
                // преобразуем в промежуточное представление
                .Select(r => ConvertToIntermediate(r, null));
            return query;
        }

        // Получение записи по ее идентификатору. Если нет, то null. По признаку addinverse добавляюся обратные ссылки 
        public XElement GetRecordBasic(string id, bool addinverse, string unused_direct_prop)
        {
            // Находим запись
            XElement rec = id == null || !recordsById.ContainsKey(id) ? null : recordsById[id];
            if (rec == null) return null;
            XElement result = ConvertToIntermediate(rec, unused_direct_prop);
            // Добавим обратные
            if (addinverse && subelementsByResource.ContainsKey(id))
            {
                result.Add(subelementsByResource[id]
                    .Select(sub =>
                    {
                        string prop = sub.Name.NamespaceName + sub.Name.LocalName;
                        XElement parent = sub.Parent;
                        return new XElement("inverse", new XAttribute("prop", prop), 
                            new XElement("record", new XAttribute("id", parent.Attribute(IEngine.rdfabout).Value)));
                    }));
            }
            return result;
        }

        public IEnumerable<RRecord> RSearch(string searchstring)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RRecord> RSearch(string searchstring, string type)
        {
            throw new NotImplementedException();
        }

        public RRecord GetRRecord(string id)
        {
            throw new NotImplementedException();
        }
        public RRecord GetRRecord(string id, bool addinverse)
        {
            throw new NotImplementedException();
        }


        public void Update(RRecord rec)
        {
            throw new NotImplementedException();
        }

        public bool DeleteRecord(string id)
        {
            throw new NotImplementedException();
        }

        public string NewRecord(string type, string name)
        {
            throw new NotImplementedException();
        }
        public string NewRelation(string type, string inverseprop, string source)
        {
            throw new NotImplementedException();
        }
        public RRecord BuildPortrait(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RRecord> RAll()
        {
            throw new NotImplementedException();
        }
    }
}
