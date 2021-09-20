using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McJenny.WebAPI.Helpers
{
    public static class StringHelpers
    {
        public static string QueryToParam(this String source, string keyword)
            => source
                .Substring(source.IndexOf(keyword+"="))
                    .Replace(keyword+"=", string.Empty)
                    .Split('&', 2).FirstOrDefault();

    }
}
