﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using NGraphQL.Model;

namespace NGraphQL.Model.Construction {

  public class XmlDocumentationLoader {
    Dictionary<Assembly, Dictionary<string, XElement>> _data = new Dictionary<Assembly, Dictionary<string, XElement>>(); 

    public bool TryLoadAssemblyXmlFile(Type type) {
      var asm = type.Assembly;
      if(_data.ContainsKey(asm))
        return true; // already loaded
      var file = asm.GetName().Name + ".xml";
      if(!File.Exists(file))
        return false; 

      //Load all member elements
      var xml = File.ReadAllText(file);
      var xDoc = XDocument.Parse(xml);
      var memberNodes = xDoc.Root.Descendants("members").First().Descendants("member").ToList();

      // Add a dictionary with all member elements for the assembly
      var dict = new Dictionary<string, XElement>();
      _data.Add(asm, dict); 
      foreach(var mElem in memberNodes) {
        var key = mElem.Attribute("name").Value;
        //for methods, cut off param list in parenthesis - we do not support overloading anyway
        //  (param types are needed to resolve overloads)
        var pIndex = key.IndexOf('(');
        if(pIndex >= 0)
          key = key.Substring(0, pIndex);
        dict[key] = mElem; 
      }

      return true;
    }

    public string GetDocString(object target, Type declaringType) {
      if(target == null)
        throw new Exception("Target may not be null");
      if(!_data.TryGetValue(declaringType.Assembly, out var dict))
        return null;
      var key = GetKey(target);
      if(!dict.TryGetValue(key, out var member))
        return null;
      var str = member.Element("summary").Value?.Trim();
      return str; 
    }

    private string GetKey(object obj) {
      switch(obj) {
        case Type t: return $"T:{t.Namespace}.{t.Name}";
        case FieldInfo f: return $"F:{FullName(f.DeclaringType)}.{f.Name}";
        case PropertyInfo p: return $"P:{FullName(p.DeclaringType)}.{p.Name}";
        case MethodInfo m: return $"M:{FullName(m.DeclaringType)}.{m.Name}";
        default:
          throw new Exception($"Invalid object type for xml doc lookup: {obj}"); 
      }
    }

    private string FullName(Type t) {
      return t.Namespace + "." + t.Name;
    }

  }
}