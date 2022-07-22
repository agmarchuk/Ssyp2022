using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RDFEngine
{
    public class PhototekaGenerator
    {
        public static IEnumerable<XElement> Generate(int npersons)
        {
            Random rnd = new Random();
            int np = npersons;
            int nf = npersons * 2;
            int nr = npersons * 6;
            IEnumerable<XElement> recordFlow = 
                Enumerable.Range(0, np)
                    .Select(i => new XElement("person", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", "p" + i),
                        new XElement("name", new XAttribute("{http://www.w3.org/XML/1998/namespace}lang", "ru"), "и" + i),
                        new XElement("age", "23")))
                    .Concat(
                        Enumerable.Range(0, nf)
                            .Select(i => new XElement("photo", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", "f" + i),
                                new XElement("name", "dsp" + i))))
                    .Concat(
                        Enumerable.Range(0, nr)
                            .Select(i => new XElement("reflection", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", "r" + i),
                                new XElement("reflected", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", "p" + rnd.Next(np))),
                                new XElement("indoc", new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", "f" + rnd.Next(nf))))));
            ;
            return recordFlow;
        }
    }
}

