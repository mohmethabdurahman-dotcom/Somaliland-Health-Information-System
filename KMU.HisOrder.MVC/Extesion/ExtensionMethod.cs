using System.ComponentModel;
using System.Reflection;

namespace KMU.HisOrder.MVC.Extesion
{
    public static class ExtensionMethod
    {
        /// <summary>
        /// 轉換DBNull為String
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        static public string DBNullToString(this object src)
        {
            string ret = "";
            if (src != null && src != DBNull.Value)
            {
                ret = src.ToString();
            }
            return ret;
        }

        /// <summary>
        /// 轉換String Null為空字串
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        static public string NullToString(this string src)
        {
            string ret = "";
            if (src != null)
            {
                ret = src;
            }
            return ret;
        }

        /// <summary>
        /// 把Enum的Type轉成字串
        /// </summary>
        /// <param name="src">Enum</param>
        /// <returns>String</returns>
        public static string EnumToString(this Enum src)
        {
            return Enum.GetName(src.GetType(), src);
        }

        /// <summary>
        /// 讀取Enum列舉項目所定義的Description 通常會是DB中實際儲存的值
        /// </summary>
        /// <param name="value">要讀description的enum項目</param>
        /// <returns>如果存在description就回傳description 不然就回傳一般的ToString()</returns>
        public static string EnumToCode(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute? attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return value.ToString();
        }

        /// <summary>
        /// 年齡顯示, 一歲以下year, 以上years
        /// </summary>
        /// <param name="inAge">年齡</param>
        /// <returns>年齡 + year/years</returns>
        /// 2023.06.15 add by elain.
        public static string AgeFormat(this int inAge)
        {
            
            var _unit = "years";
            if (inAge <= 1)
            {
                _unit = "year";
            }

            return string.Format("{0} {1}", inAge.ToString(), _unit);
        }
    }
}
