using AutoMapper;
using KMU.HisOrder.MVC.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Models
{
    public class SoapService : IDisposable
    {
        private readonly KMUContext _context;

        public SoapService(KMUContext context)
        {
            _context = context;
        }

        public void Dispose() => Dispose(disposing: true);

        /// <summary>
        /// Releases all resources currently used by this <see cref="Controller"/> instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being invoked by the <see cref="Dispose()"/> method,
        /// otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        public List<Hisordersoa> getSoapData(string inhospid, string inSourceType, int? targetVer = null)
        { 
            var soapData = new List<Hisordersoa>();
            if (string.IsNullOrWhiteSpace(inSourceType))
            {
                inSourceType = "OPD";
            }

            if (inSourceType == "OPD")
            {
                soapData = _context.Hisordersoas.Where(c => c.Inhospid == inhospid && c.Status != 'X').ToList();
            }
            else
            {
                if (targetVer == null)
                {
                    var dbData = _context.Hisordersoas.Where(c => c.Inhospid == inhospid && c.Status != 'X').ToList();

                    if (dbData.Any())
                    {
                        var maxVersion = dbData.Max(d => d.VersionCode).Value;
                        //ER
                        soapData = _context.Hisordersoas.Where(c => c.Inhospid == inhospid && c.Status != 'X' && c.VersionCode == maxVersion).ToList();
                    }
                }
                else
                {
                    soapData = _context.Hisordersoas.Where(c => c.Inhospid == inhospid && c.Status != 'X' && c.VersionCode == targetVer).ToList();
                }
            }

            if (soapData.Any())
            {
                return soapData.ToList();
            }
            else
            {
                return new List<Hisordersoa>();
            }
        }

        internal string StripHTML(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            //空白字元特殊處理
            input = input.Replace("&nbsp;", " ").Replace("&ensp;", " ").Replace("&emsp;", "　");
      
            input = input.Replace("\x0a\x0a", "\x0a");  //換行字元同\n
            //Regex.Replace(input, "<.*?>", String.Empty);
            string noTag = Regex.Replace(input, @"<[^>]+>|&nbsp;", String.Empty).Trim();    //去除HTML Tag
            return WebUtility.HtmlDecode(noTag);    //HtmlDecode : 原html符號字元需呈現(ex:&)
        }

    }
}
