using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Components.Serialize.Protobuf;
using Components.Extensions;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace Components.Serialize
{
    public class Serializer
    {
        #region Protobuf
        public static string ProtobufStringSerialize(object obj)
        {
            if (obj != null)
            {
                return Encoding.UTF8.GetString(ProtobufByteSerialize(obj));
            }

            return null;
        }

        public static byte[] ProtobufByteSerialize(object obj)
        {
            if (obj != null)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    ProtobufSerialize(obj, stream);
                    long length = stream.Length;
                    byte[] buffer = new byte[length];
                    stream.Seek(0L, SeekOrigin.Begin);
                    stream.Read(buffer, 0, Convert.ToInt32(length));
                    stream.Close();
                    return buffer;
                }
            }
            return null;
        }

        private static void ProtobufSerialize(object obj, Stream stream)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is DataSet)
            {
                ProtobufDataSetSerializer.ProtoWrite(obj as DataSet, stream);
            }
            else if (obj is DataTable)
            {
                ProtobufDataTableSerializer.ProtoWrite(obj as DataTable, stream);
            }
            else
            {
                ProtobufSerializer.Serialize<object>(stream, obj);
            }
        }

        public static T ProtobufByteDeSerialize<T>(byte[] src)
        {
            if (src == null || src.Length < 1)
            {
                return default(T);
            }
            using (MemoryStream ms = new MemoryStream(src))
            {
                return ProtobufDeSerialize<T>(ms);
            }
        }

        public static T ProtobufStringDeSerialize<T>(string src)
        {
            if (src.IsEmpty())
            {
                return default(T);
            }

            return ProtobufByteDeSerialize<T>(Convert.FromBase64String(src));

        }

        private static T ProtobufDeSerialize<T>(Stream src)
        {
            if (src == null)
            {
                return default(T);
            }

            if (typeof(T) == typeof(DataSet))
            {
                return (T)Convert.ChangeType(ProtobufDataSetSerializer.ProtoRead(src), typeof(T));
            }

            if (typeof(T) == typeof(DataTable))
            {
                return (T)Convert.ChangeType(ProtobufDataTableSerializer.ProtoRead(src), typeof(T));
            }

            T obj = ProtobufSerializer.Deserialize<T>(src);
            if (obj == null)
            {
                return default(T);
            }

            return obj;
        }
        #endregion

        #region Xml
        public static string XmlStringSerialize<T>(T obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringWriter sw = new StringWriter())
            {
                xmlSerializer.Serialize(sw, obj);
                return sw.ToString();
            }
        }

        public static byte[] XmlByteSerialize<T>(T obj)
        {
            if (obj == null)
            {
                return null;
            }
            return Encoding.UTF8.GetBytes(XmlStringSerialize<T>(obj));
        }

        public static T XmlByteDeserialize<T>(byte[] data)
        {
            if (data == null || data.Length < 1)
            {
                return default(T);
            }
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(data))
            using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
            {
                return (T)xmlSerializer.Deserialize(reader);
            }
        }

        public static T XmlStringDeserialize<T>(string data)
        {
            if (data.IsEmpty())
            {
                return default(T);
            }
            return XmlByteDeserialize<T>(Encoding.UTF8.GetBytes(data));
        }
        #endregion

        #region Binary
        public static byte[] BinaryByteSerialize(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static string BinaryStringSerialize(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(BinaryByteSerialize(obj));
        }

        public static T BinaryByteDeserialize<T>(byte[] data)
        {
            if (data == null || data.Length < 1)
            {
                return default(T);
            }
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return (T)bf.Deserialize(ms);
            }
        }

        public static T BinaryStringDeserialize<T>(string data)
        {
            if (data.IsEmpty())
            {
                return default(T);
            }
            return BinaryByteDeserialize<T>(Encoding.UTF8.GetBytes(data));
        }
        #endregion

        #region JSON

        public static string JSONSerialize<T>(T obj)
        {
            string result = string.Empty;
            //序列化
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                result = Encoding.UTF8.GetString(ms.ToArray());
            }
            //替换Json的Date字符串
            string pattern = @"///Date/((/d+)/+/d+/)///"; /*////Date/((([/+/-]/d+)|(/d+))[/+/-]/d+/)////*/
            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
            Regex reg = new Regex(pattern);
            result = reg.Replace(result, matchEvaluator);
            return result;
        }

        public static T JSONDeserialize<T>(string data)
        {
            string str = string.Empty;
            //将"yyyy-MM-dd HH:mm:ss"格式的字符串转为"//Date(1294499956278+0800)//"格式  
            string pattern = @"/d{4}-/d{2}-/d{2}/s/d{2}:/d{2}:/d{2}";
            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertDateStringToJsonDate);
            Regex reg = new Regex(pattern);
            str = reg.Replace(data, matchEvaluator);
            //反序列化
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                return (T)serializer.ReadObject(ms);
            }
        }

        /// <summary>  
        /// 将Json序列化的时间由/Date(1294499956278+0800)转为字符串  
        /// </summary>  
        private static string ConvertJsonDateToDateString(Match match)
        {
            DateTime dt = new DateTime(1970, 1, 1);
            dt = dt.AddMilliseconds(long.Parse(match.Groups[1].Value));
            dt = dt.ToLocalTime();
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>  
        /// 将时间字符串转为Json时间  
        /// </summary>  
        private static string ConvertDateStringToJsonDate(Match match)
        {
            DateTime dt = DateTime.Parse(match.Groups[0].Value);
            dt = dt.ToUniversalTime();
            TimeSpan ts = dt - DateTime.Parse("1970-01-01");
            return string.Format("///Date({0}+0800)///", ts.TotalMilliseconds);
        }
        #endregion
    }
}
