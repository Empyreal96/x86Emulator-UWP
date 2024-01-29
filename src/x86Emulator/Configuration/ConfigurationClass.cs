using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x86Emulator.Configuration
{
    public enum ConfigurationUserLevel
    {
        None,
        PerUserRoaming,
        PerUserRoamingAndLocal
    }
    
    public enum ConfigurationPropertyOptions
    {
        IsAssemblyStringTransformationRequired,
        IsDefaultCollection,
        IsKey,
        IsRequired,
        IsTypeStringTransformationRequired,
        IsVersionCheckRequired,
        None
    }

    public class Configuration
    {
        public Sections Sections = new Sections();

        public object GetSection(string name)
        {
            return Sections.GetSection(name);
        }
        public void Save()
        {
            //Later
        }
    }
    public static class ConfigurationManager
    {
        public static Configuration GlobalConfiguration = new Configuration();
        public static Configuration OpenExeConfiguration(ConfigurationUserLevel level)
        {
            return GlobalConfiguration;
        }
    }

    public class Sections
    {
        public List<Section> sections = new List<Section>();
        public void Add(string name, object section)
        {
            sections.Add(new Section(name, section));
        }
        public object GetSection(string name)
        {
            foreach(var section in sections)
            {
                if (section.name.Equals(name))
                {
                    return section.section;
                }
            }
            return null;
        }
    }
    public class Section
    {
        public string name;
        public object section;
        public Section(string name, object section)
        {
            this.name = name;
            this.section = section;
        }
    }
    public class ConfigurationProperty
    {
        public Type type;
        public string name;
        public object value;
        public ConfigurationProperty(string name, Type type, object value, ConfigurationPropertyOptions level)
        {
            this.type = type;    
            this.name = name;
            this.value = value;
        }
    }
    public class ConfigurationPropertyCollection
    {
        public List<ConfigurationProperty> properties = new List<ConfigurationProperty>();

        public void Add(ConfigurationProperty configurationProperty)
        {
            properties.Add(configurationProperty);
        }
        public ConfigurationProperty Get(string name)
        {
            foreach(var property in properties)
            {
                if (property.name.Equals(name))
                {
                    return property;
                }
            }

            return null;
        }

        public void Set(string name, object value)
        {
            foreach (var property in properties)
            {
                if (property.name.Equals(name))
                {
                    property.value = value;
                    break;
                }
            }
        }
    }

    public class SectionInformation
    {
        public bool ForceSave = false;
    }
}
