using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Common
{
    public static class Serializer
    {
        public static void Serialize(this object obj, string path)
        {
            using (FileStream fs = File.Open(path, FileMode.Create))
                ProtoBuf.Serializer.Serialize(fs, obj);
        }

        public static T Deserialize<T>(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
                return ProtoBuf.Serializer.Deserialize<T>(fs);
        }

        public static void SerializeXml(this object obj, string path)
        {
            XmlSerializer writer = new XmlSerializer(obj.GetType());

            using (FileStream file = File.Create(path))
                writer.Serialize(file, obj);
        }

        public static T DeserializeXml<T>(string path)
        {
            XmlSerializer reader = new XmlSerializer(typeof(T));

            using (FileStream file = File.OpenRead(path))
                return (T)reader.Deserialize(file);
        }

        public static void SerializeJson(this object obj, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
        }

        public static T DeserializeJson<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
    }
}
