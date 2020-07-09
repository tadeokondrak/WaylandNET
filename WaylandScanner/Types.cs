using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace WaylandScanner
{
    [XmlRoot("protocol")]
    public class Protocol
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("copyright")]
        public Copyright Copyright { get; set; }

        [XmlElement("description")]
        public Description Description { get; set; }

        [XmlElement("interface")]
        public List<Interface> Interfaces { get; set; }
    }

    public class Copyright
    {
        [XmlText]
        public string Content { get; set; }
    }

    public class Description
    {
        [XmlAttribute("summary")]
        public string Summary { get; set; }

        [XmlText]
        public string Content { get; set; }
    }

    public class Interface
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("version")]
        public uint Version { get; set; }

        [XmlElement("description")]
        public Description Description { get; set; }

        [XmlElement("request")]
        public List<Message> Requests { get; set; }

        [XmlElement("event")]
        public List<Message> Events { get; set; }

        [XmlElement("enum")]
        public List<Enum> Enums { get; set; }
    }

    public class Message
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("since")]
        public uint Since { get; set; }

        [XmlElement("description")]
        public Description Description { get; set; }

        [XmlElement("arg")]
        public List<Argument> Arguments { get; set; }
    }

    public class Enum
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("since")]
        public uint Since { get; set; }

        [XmlAttribute("bitfield")]
        public bool Bitfield { get; set; }

        [XmlElement("description")]
        public Description Description { get; set; }

        [XmlElement("entry")]
        public List<Entry> Entries { get; set; }
    }

    public class Argument
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("summary")]
        public string Summary { get; set; }

        [XmlAttribute("interface")]
        public string Interface { get; set; }

        [XmlAttribute("allow_null")]
        public bool AllowNull { get; set; }

        [XmlAttribute("enum")]
        public string Enum { get; set; }
    }

    public class Entry
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("summary")]
        public string Summary { get; set; }

        [XmlAttribute("since")]
        public uint Since { get; set; }
    }

}
