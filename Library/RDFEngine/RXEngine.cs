using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RDFEngine
{
    public class RXEngine : IEngine
    {
        public string User { get; set; }
        public bool All { get; set; }
        public void Build()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RRecord> RSearch(string searchstring)
        {
            if (string.IsNullOrEmpty(searchstring))
            {
                return new RRecord[0];
            }
            var res = OAData.OADB.SearchByName(searchstring)
                .Select(x => new RRecord
                {
                    Id = x.Attribute("id").Value,
                    Tp = x.Attribute("type").Value,
                    Props = x.Elements()
                        .Select(e =>
                        {
                            if (e.Name == "field") return new RField { Prop = e.Attribute("prop").Value, Value = e.Value };
                            return null;
                        }).ToArray()

                }).ToArray();
            return res;
        }

        public IEnumerable<RRecord> RSearch(string searchstring, string type)
        {
            return RSearch(searchstring).Where(r => r.Tp == type);
        }
        public IEnumerable<RRecord> RSearchByWords(string searchstring)
        {
            var res = OAData.OADB.SearchByWords(searchstring)
                .Select(x => new RRecord
                {
                    Id = x.Attribute("id").Value,
                    Tp = x.Attribute("type").Value,
                    Props = x.Elements()
                        .Select(e =>
                        {
                            if (e.Name == "field") return new RField { Prop = e.Attribute("prop").Value, Value = e.Value };
                            return null;
                        }).ToArray()

                }).ToArray();
            return res;
        }
        public IEnumerable<RRecord> RAll()
        {
            var res = OAData.OADB.SearchByName("")
                .Select(x => new RRecord
                {
                    Id = x.Attribute("id").Value,
                    Tp = x.Attribute("type").Value,
                    Props = x.Elements()
                        .Select(e =>
                        {
                            if (e.Name == "field") return new RField { Prop = e.Attribute("prop").Value, Value = e.Value };
                            return null;
                        }).ToArray()

                }).ToArray();
            return res;
        }

        public RRecord BuildPortrait(string id)
        {
            return BuPo(id, 2, null);
        }
        private RRecord BuPo(string id, int level, string forbidden)
        {
            var rec = GetRRecord(id, true);
            if (rec == null) return null;
            RRecord result_rec = new RRecord()
            {
                Id = rec.Id,
                Tp = rec.Tp,
                Props = rec.Props.Select<RProperty, RProperty>(p =>
                {
                    if (p is RField)
                        return new RField() { Prop = p.Prop, Value = ((RField)p).Value, Lang = ((RField)p).Lang == null ? null : ((RField)p).Lang };
                    else if (level > 0 && p is RLink && p.Prop != forbidden)
                        return new RDirect() { Prop = p.Prop, DRec = BuPo(((RLink)p).Resource, 0, null) };
                    else if (level > 1 && p is RInverseLink)
                        return new RInverse() { Prop = p.Prop, IRec = BuPo(((RInverseLink)p).Source, 1, p.Prop) };
                    return null;
                }).Where(p => p != null).ToArray()
            };
            return result_rec;
        }
        public RRecord GetRRecord(string id)
        {
            if (id == null) return null;
            return GetRRecord(id, false);
        }
        public RRecord GetRRecord(string id, bool addinverse)
        {
            var item = OAData.OADB.GetItemByIdBasic(id, addinverse);
            if (item == null) return null;
            RRecord rec = new RRecord
            {
                Id = item.Attribute("id").Value,
                Tp = item.Attribute("type").Value,
                Props = item.Elements().Select(px =>
                {
                    if (px.Name == "field")
                    {
                        return new RField 
                        { Prop = px.Attribute("prop").Value, Value = px.Value, 
                            Lang = px.Attribute("{http://www.w3.org/XML/1998/namespace}lang")?.Value };
                    }
                    else if (px.Name == "direct")
                    {
                        RLink rl = new RLink
                        {
                            Prop = px.Attribute("prop").Value,
                            Resource = px.Element("record").Attribute("id").Value
                        };
                        return rl;
                    }
                    else if (px.Name == "inverse")
                    {
                        RInverseLink ril = new RInverseLink
                        {
                            Prop = px.Attribute("prop").Value,
                            Source = px.Element("record").Attribute("id").Value
                        };
                        return ril;
                    }
                    else
                    {
                        return (RProperty)null;
                    }
                })
                .Where(p => p != null)
                .ToArray()
            };
            return rec;
        }

        public void Load(IEnumerable<XElement> records)
        {
            throw new NotImplementedException();
        }


        // ================= Группа редактирования ==================

        public bool DeleteRecord(string id)
        {
            var res = OAData.OADB.PutItem(new XElement("delete",
                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id),
                new XAttribute("owner", User)));
            if (res.Name == "error") throw new Exception(res.Value);
            return res != null ? true : false;
        }

        public bool DeleteRecord(string id, string user)
        {
            var res = OAData.OADB.PutItem(new XElement("delete",
                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id),
                new XAttribute("owner", user)));
            if (res.Name == "error") throw new Exception(res.Value);
            return res != null ? true : false;
        }

        private XName ToXName(string name)
        {
            int pos = name.LastIndexOf('/'); //TODO: Наверное, нужны еще другие окончания пространств имен
            string localName = name.Substring(pos + 1);
            string namespaceName = pos >= 0 ? name.Substring(0, pos + 1) : "";
            return XName.Get(localName, namespaceName);
        }

        public string NewRecord(string type, string name)
        {
            var res = OAData.OADB.PutItem(
                new XElement(ToXName(type),
                    new XElement("{http://fogid.net/o/}name", name),
                    new XAttribute("owner", User)));
            if (res.Name == "error") throw new Exception(res.Value);
            return res.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        }

        public string NewRecord(string type, string name, string user)
        {
            var res = OAData.OADB.PutItem(
                new XElement(ToXName(type),
                    new XElement("{http://fogid.net/o/}name", name),
                    new XAttribute("owner", user)));
            if (res.Name == "error") throw new Exception(res.Value);
            return res.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        }

        public string NewRelation(string type, string inverseprop, string source)
        {
            var x1 = ToXName(type);
            var x2 = ToXName(inverseprop);
            //var res = OAData.OADB.PutItem(
            //    new XElement(ToXName(type),
            //        new XElement(ToXName(inverseprop),
            //            new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", source)),
            //        new XAttribute("owner", User)));
            var res = OAData.OADB.PutItem(new XElement(x1, new XElement(x2, new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", source)),
                new XAttribute("owner", User)));

            if (res.Name == "error") throw new Exception(res.Value);
            return res.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        }

        public string NewRelation(string type, string inverseprop, string source, string user)
        {
            var x1 = ToXName(type);
            var x2 = ToXName(inverseprop);
            //var res = OAData.OADB.PutItem(
            //    new XElement(ToXName(type),
            //        new XElement(ToXName(inverseprop),
            //            new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", source)),
            //        new XAttribute("owner", User)));
            var res = OAData.OADB.PutItem(new XElement(x1, new XElement(x2, new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", source)),
                new XAttribute("owner", user)));

            if (res.Name == "error") throw new Exception(res.Value);
            return res.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        }


        public void Update(RRecord record)
        {
            Update(record, null);
        }

        public void Update(RRecord record, string user)
        {
            var xres = new XElement(ToXName(record.Tp),
                    (record.Id == null ? null : new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", record.Id)),
                    record.Props.Select(p =>
                    {
                        if (p is RField)
                        {
                            return new XElement(ToXName(p.Prop), ((RField)p).Value,
                                ((RField)p).Lang == null ? null : new XAttribute("{http://www.w3.org/XML/1998/namespace}lang", ((RField)p).Lang));
                        }
                        else if (p is RLink)
                        {
                            return new XElement(ToXName(p.Prop),
                                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", ((RLink)p).Resource));
                        }
                        else if (p is RDirect)
                        {
                            if (((RDirect)p).DRec == null) return null;
                            return new XElement(ToXName(p.Prop),
                                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", ((RDirect)p).DRec.Id));
                        }
                        return null;
                    }).Where(x => x != null),
                    new XAttribute("owner", User));
            var res = OAData.OADB.UpdateItem(xres);
            if (res.Name == "error") throw new Exception(res.Value);
        }
    }
}
