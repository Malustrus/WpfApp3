using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3
{
    public static class ExcelNameHelper
    {
        public static string GetColumnName(int index)
        {
            string name = "";
            while (index >= 0)
            {
                name = (char)('A' + index % 26) + name;
                index = index / 26 - 1;
            }
            return name;
        }
    }
}
