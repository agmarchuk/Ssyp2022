using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace RDFEngine
{
    public interface IEngine
    {
        public void Clear();
        public void Load(IEnumerable<XElement> records);
        public void Build();
        public IEnumerable<RRecord> RSearch(string searchstring);
        public IEnumerable<RRecord> RAll();

        public IEnumerable<RRecord> RSearch(string searchstring, string type);
        public RRecord GetRRecord(string id);
        public RRecord GetRRecord(string id, bool addinverse);

        // Не уверен, что нужно...
        public RRecord BuildPortrait(string id);

        public void Update(RRecord record);
        public bool DeleteRecord(string id); // возвращает true если успешно
        public string NewRecord(string type, string name); // возвращает идентификаторо созданной записи 
        public string NewRelation(string type, string inverseprop, string source);

        // Константы для удобства работы с RDF/XML
        public static XName rdfabout = XName.Get("about", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        public static XName rdfresource = XName.Get("resource", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

    }
}
