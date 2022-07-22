using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDFEngine
{
    public class RRecord
    {
        public string Id { get; set; }
        public string Tp { get; set; }
        public RProperty[] Props { get; set; }
        //public string Label { get; set; } // поле понадобится для хранения метки
        //public override string ToString()
        //{
        //    var query = Props.Select(p =>
        //    {
        //        string prop = p.Prop;
        //        if (p is RField)      return "f^{<" + prop + ">, \"" + ((RField)p).Value + "\"}";
        //        else if (p is RLink) return "l^{<" + prop + ">, <" + ((RLink)p).Resource + ">}";
        //        // Добавленный вариант обратной ссылки
        //        else if (p is RInverseLink) return "il^{<" + prop + ">, <" + ((RInverseLink)p).Source + ">}";
        //        else if (p is RDirect) return "d^{<" + prop + ">, " + ((RDirect)p).DRec.ToString() + "}";
        //        /*else if (p is RDirect)*/ return "i^{<" + prop + ">, " + ((RInverse)p).IRec.ToString() + "}";
        //    }).Aggregate((a, s) => a + ", " + s);
        //    return "{ <" + Id + ">, <" + Tp + ">, " + "[" +       query      + "]}";
        //}
        public string GetField(string propName)
        {
            return ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == propName))?.Value;
        }
        public string GetField(int propind)
        {
            return ((RField)this.Props[propind])?.Value;
        }
        public string GetDirectResource(string propName)
        {
            var prop = this.Props.FirstOrDefault(p => p.Prop == propName);
            if (prop == null) return null;
            if (prop is RLink) return ((RLink)prop).Resource;
            if (prop is RDirect) return ((RDirect)prop).DRec?.Id;
            return null;
        }
        public RRecord GetDirect(string propName)
        {
            if (propName == null) return null;
            var prop = this.Props.FirstOrDefault(p => p?.Prop == propName);
            if (prop == null) return null;
            if (prop is RDirect) return ((RDirect)prop).DRec;
            return null;
        }
        public RRecord GetDirect(int propind)
        {
            var prop = Props[propind];
            if (prop == null) return null;
            if (prop is RDirect) return ((RDirect)prop).DRec;
            return null;
        }
        public RRecord[] GetMultiInverse(int propind)
        {
            var prop = Props[propind];
            if (prop == null) return new RRecord[0];
            if (prop is RMultiInverse) return ((RMultiInverse)prop).IRecs;
            return new RRecord[0];
        }

        public string GetName()
        {
            return ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == REngine.propName))?.Value;
        }
        public string GetName(string lang)
        {
            var name = ((RField)this.Props.FirstOrDefault(p => p is RField && ((RField)p).Lang == lang && p.Prop == REngine.propName));
            if (name != null)
            {
                return name.Value;
            }
            else
            {
                name = ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == REngine.propName));
                var langName = (name.Lang == null) ? "ru" : name.Lang;
                if (langName != lang)
                {
                    return name.Value + " (" + langName + ")";
                } else
                {
                    return name.Value;
                }
            }
        }
        public string GetDates()
        {
            string df = GetField("http://fogid.net/o/from-date");
            string dt = GetField("http://fogid.net/o/to-date");
            return (df == null ? "" : df) + (string.IsNullOrEmpty(dt) ? "" : "-" + dt);
        }
    }
    public abstract class RProperty
    {
        public string Prop { get; set; }
    }
    public class RField : RProperty 
    {
        public string Value { get; set; }
        public string Lang { get; set; }
    }
    public class RLink : RProperty, IEquatable<RLink>
    {
        public string Resource { get; set; }

        public bool Equals(RLink other)
        {
            return this.Prop == other.Prop && this.Resource == other.Resource;
        }

        public int GetHashCode([DisallowNull] RLink obj)
        {
            return obj.Prop.GetHashCode() ^ obj.Resource.GetHashCode();
        }
    }


    // Custom comparer for the RRecord class
    public class RRecordComparer : IEqualityComparer<RRecord>
    {
        public bool Equals(RRecord x, RRecord y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;
            return x.Id == y.Id;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode([DisallowNull] RRecord obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;
            return obj.Id.GetHashCode();
        }
    }

    // Расширение вводится на странице 11 пособия "Делаем фактографию"
    public class RInverseLink : RProperty
    {
        public string Source { get; set; }
    }

    // Новое расширение
    public class RDirect : RProperty
    {
        public RRecord DRec { get; set; }
    }
    public class RInverse : RProperty
    {
        public RRecord IRec { get; set; }
    }

    // Еще более новое расширение
    public class RMultiInverse : RProperty
    {
        public RRecord[] IRecs { get; set; }
    }

    // Специальное расширение для описателей перечислимых  
    public class RState : RProperty
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string lang { get; set; }
    }




}
