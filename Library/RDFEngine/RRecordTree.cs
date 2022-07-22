using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDFEngine
{
    public class RRecordTree
    {
        private RRecord record = null;
        public RRecordTree(string recId, ROntology rontology)
        {
            this.rontology = rontology;
            record = Do(recId, 2, null);
        }
        public string Id { get { return record.Id; } }
        public string Tp { get { return record.Tp; } }
        public RProperty[] Props { get { return record.Props; } }
        public string GetField(string propId)
        {
            int ind = rontology.PropPosition(Tp, propId, false);
            if (ind == -1) return null;
            return ((RField)record.Props[ind])?.Value;
        }
        public string GetName() => GetField("http://fogid.net/o/name");
        public string GetDates()
        {
            string fd = GetField("http://fogid.net/o/from-date");
            string td = GetField("http://fogid.net/o/to-date");
            return "" + fd ?? "" + (td == null ? "" : "-" + td);
        }
        public string GetLabel(string ontoTerm) => rontology.LabelOfOnto(ontoTerm);
        public RRecord GetDirect(string propId)
        {
            int ind = rontology.PropPosition(Tp, propId, false);
            if (ind == -1) return null;
            return record.GetDirect(propId);
        }
        public RRecord[] GetMultiInverse(string propId)
        {
            int ind = rontology.PropPosition(Tp, propId, true);
            if (ind == -1) return new RRecord[0];
            return record.GetMultiInverse(ind);
        }

        private ROntology rontology;
        private RRecord Do(string recId, int level, string forbidden)
        {
            // Если level = 0 - только поля, 1 - поля и прямые ссылки,  2 - поля, прямые ссылки и обратные ссылки
            RRecord erec = (new RDFEngine.RXEngine()).GetRRecord(recId, level > 1);
            if (erec == null) return null;
            var tp = erec.Tp;

            // В зависимости от типа, узнаем количество прямых и обратных свойств и заводим массив свойств этого размера  
            int nprops = rontology.PropsTotal(tp);
            RProperty[] props = new RProperty[nprops];

            // Также заводим массив списков RRecord'ов для накопления сгруппированных обратных свойств
            List<RRecord>[] reclists = new List<RRecord>[nprops];

            // Сканируем имеющиеся свойства записи и раскладываем их по массиву в соответствии с позицией ind, для обратных свойств пока накапливаем  
            foreach (var p in erec.Props)
            {
                if (p is RLink && p.Prop == forbidden) continue;
                int ind = rontology.PropPosition(tp, p.Prop, p is RInverseLink);
                if (ind == -1) continue;
                if (p is RField)
                {
                    props[ind] = p;
                }
                else if (p is RLink)
                {
                    if (level == 0) continue;
                    props[ind] = new RDirect { Prop = p.Prop, DRec = Do(((RLink)p).Resource, level - 1, null) };
                }
                else if (p is RInverseLink)
                {
                    // накапливаем
                    if (level < 2) continue;
                    var lnk = (RInverseLink)p;
                    RRecord rec = Do(lnk.Source, level - 1, lnk.Prop);
                    if (reclists[ind] == null)
                    {
                        reclists[ind] = new List<RRecord>();
                    }
                    reclists[ind].Add(rec);
                    // В результирующем массиве, сохраняем p для того, чтобы не "потерять" Prop 
                    props[ind] = p;
                }
                else
                {
                    throw new Exception("Err: 29282");
                }
            }

            // Проходим по вспомогатльному массиву и раскладываем обратные накопления  
            for (int i = 0; i < nprops; i++)
            {
                if (reclists[i] == null) continue;
                props[i] = new RMultiInverse { Prop = props[i].Prop, IRecs = reclists[i].ToArray() };
            }
            erec = new RRecord { Id = erec.Id, Tp = erec.Tp, Props = props };
            return erec;
        }

    }
}
