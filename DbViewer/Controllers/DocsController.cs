using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbViewer
{
    public class DocsController : Controller
    {
        [HttpGet("docs/GetImage")]
        public IActionResult GetImage(string u, string s)
        {
            string path = OAData.OADB.GetFilePath(u, s);
            if (!System.IO.File.Exists(path + ".jpg"))
            {
                s = s == "medium" ? "normal" : "medium";
                path = OAData.OADB.GetFilePath(u, s);
            }
            return PhysicalFile(path + ".jpg", "image/jpg");
        }

        [HttpGet("docs/GetVideo")]
        public IActionResult GetVideo(string u)
        {
            string path = OAData.OADB.GetFilePath(u, "medium");
            if (path == null) return NotFound();
            string dir_path = path.Substring(0, path.Length - 5);
            string file_num = path.Substring(path.Length - 4);
            System.IO.DirectoryInfo dinfo = new System.IO.DirectoryInfo(dir_path);
            var qu = dinfo.GetFiles(file_num + ".*");
            if (qu.Length == 0) return NotFound();
            int pos = qu[0].Name.LastIndexOf('.');
            if (pos == -1) return NotFound();
            string ext = qu[0].Name.Substring(pos + 1);
            return PhysicalFile(path + "." + ext, "video/" + ext);
        }
        [HttpGet("docs/GetPdf")]
        public IActionResult GetPdf(string u)
        {
            string path = OAData.OADB.GetFilePath(u, null);
            if (path == null) return NotFound();
            return PhysicalFile(path + ".pdf", "application/pdf");
        }

    }
}
