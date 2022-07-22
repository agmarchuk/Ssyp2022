using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace CManager
{
    class RDFDocs : ObservableCollection<RDFDocFields>
    {
    }
    class RDFDocFields
    {
        public string Id { get; set; }
        public string Uri { get; set; }
        public string Owner { get; set; }
    }
}
