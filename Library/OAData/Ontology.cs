using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using OAData.Adapters;

namespace OAData
{
    public class Ontology
    {
        public static void Init(string ontologypath)
        {
            XElement ontology = XElement.Load(ontologypath);
            LoadOntNamesFromOntology(ontology);
            LoadInvOntNamesFromOntology(ontology);
        }
        public static string GetOntName(string name)
        {
            string res = null;
            if (OntNames.TryGetValue(name, out res)) return res;
            return name;
        }
        public static string GetInvOntName(string name)
        {
            string res = null;
            if (InvOntNames.TryGetValue(name, out res)) return res;
            return name;
        }

        private static Dictionary<string, string> OntNames { get; set; }
        private static Dictionary<string, string> InvOntNames { get; set; }
        private static XElement _ontology;
        public static void LoadOntNamesFromOntology(XElement ontology)
        {
            _ontology = ontology;
            var ont_names = ontology.Elements()
                .Where(el => el.Name == "Class" || el.Name == "ObjectProperty" || el.Name == "DatatypeProperty")
                .Where(el => el.Elements("label").Any())
                .Select(el => new
                {
                    type_id = el.Attribute(ONames.rdfabout).Value,
                    label = el.Elements("label").First(lab => lab.Attribute(ONames.xmllang).Value == "ru").Value
                })
                .ToDictionary(pa => pa.type_id, pa => pa.label);
            OntNames = ont_names;
        }
        public static void LoadInvOntNamesFromOntology(XElement ontology)
        {
            _ontology = ontology;
            var i_ont_names = ontology.Elements("ObjectProperty")
                //.Where(el => el.Name == "Class" || el.Name == "ObjectProperty" || el.Name == "DatatypeProperty")
                .Where(el => el.Elements("inverse-label").Any())
                .Select(el => new
                {
                    type_id = el.Attribute(ONames.rdfabout).Value,
                    label = el.Elements("inverse-label").First(lab => lab.Attribute(ONames.xmllang).Value == "ru").Value
                })
                .ToDictionary(pa => pa.type_id, pa => pa.label);
            InvOntNames = i_ont_names;
        }
        public static string GetEnumStateLabel(string enum_type, string state_value)
        {
            var et_def = _ontology.Elements("EnumerationType")
                .FirstOrDefault(et => et.Attribute(ONames.rdfabout).Value == enum_type);
            if (et_def == null) return "";
            var et_state = et_def.Elements("state")
                .FirstOrDefault(st => st.Attribute("value").Value == state_value && st.Attribute(ONames.xmllang).Value == "ru");
            if (et_state == null) return "";
            return et_state.Value;
        }
        public static IEnumerable<XElement> GetEnumStates(string enum_type)
        {
            var et_def = _ontology.Elements("EnumerationType")
                .FirstOrDefault(et => et.Attribute(ONames.rdfabout).Value == enum_type);
            if (et_def == null) return null;
            var et_states = et_def.Elements("state")
                .Where(st => st.Attribute(ONames.xmllang).Value == "ru");
            return et_states;
        }
    }
}
