using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;

namespace KartRider.Common.Data;

public class BmlObject
{
    public BmlObject()
    {
        Name = string.Empty;
        Value = string.Empty;
        Values = new Dictionary<string, string>();
        SubObjects = new List<Tuple<string, BmlObject>>();
    }

    public BmlObject(BmlObject copyObject)
        : this()
    {
        Name = copyObject.Name;
        Value = copyObject.Value;
        Values = new Dictionary<string, string>(copyObject.Values);
        foreach (var subObject in copyObject.SubObjects)
            SubObjects.Add(new Tuple<string, BmlObject>(subObject.Item1, new BmlObject(subObject.Item2)));
    }

    public BmlObject(InPacket p)
        : this()
    {
        Name = p.ReadString();
        Value = p.ReadString();
        var num = p.ReadInt();
        for (var i = 0; i < num; i++)
        {
            var key = p.ReadString();
            var value = p.ReadString();
            Values.Add(key, value);
        }

        var num2 = p.ReadInt();
        for (var j = 0; j < num2; j++)
        {
            var bmlObject = new BmlObject(p);
            SubObjects.Add(new Tuple<string, BmlObject>(bmlObject.Name, bmlObject));
        }
    }

    public string Name { get; set; }

    public string Value { get; set; }

    public Dictionary<string, string> Values { get; }

    public List<Tuple<string, BmlObject>> SubObjects { get; }

    public BmlObject Copy()
    {
        return new BmlObject(this);
    }

    public static BmlObject Create(string path)
    {
        if (!File.Exists(path)) throw new Exception("Unable to locate object file.");

        var packet = File.ReadAllBytes(path);
        var p = new InPacket(packet);
        return new BmlObject(p);
    }

    public void Save(OutPacket p)
    {
        p.WriteString(Name);
        p.WriteString(Value);
        p.WriteInt(Values.Count);
        foreach (var value in Values)
        {
            p.WriteString(value.Key);
            p.WriteString(value.Value);
        }

        p.WriteInt(SubObjects.Count);
        foreach (var subObject in SubObjects) subObject.Item2.Save(p);
    }

    public BmlObject GetObject(string name)
    {
        foreach (var subObject in SubObjects)
            if (subObject.Item1 == name)
                return subObject.Item2;

        return null;
    }

    public string GetString(string key, string def = "")
    {
        if (Values.ContainsKey(key)) return Values[key];

        return def;
    }

    public bool GetBool(string key, bool def = false)
    {
        var text = GetString(key, def.ToString()).ToLower();
        if (text == "1" || text == "true") return true;

        if (text == "0" || text == "false") return false;

        return def;
    }

    public byte GetByte(string key, byte def = 0)
    {
        var @string = GetString(key, def.ToString());
        return byte.Parse(@string);
    }

    public short GetShort(string key, int def = 0)
    {
        var @string = GetString(key, def.ToString());
        return short.Parse(@string);
    }

    public ushort GetUShort(string key, int def = 0)
    {
        var @string = GetString(key, def.ToString());
        return ushort.Parse(@string);
    }

    public int GetInt(string key, int def = 0)
    {
        var @string = GetString(key, def.ToString());
        return int.Parse(@string);
    }

    public uint GetUInt(string key, int def = 0)
    {
        var @string = GetString(key, def.ToString());
        return uint.Parse(@string);
    }

    public float GetFloat(string key, float def = 0f)
    {
        var @string = GetString(key, def.ToString());
        return float.Parse(@string);
    }

    public void SetKeyValuePair(string key, string value)
    {
        if (Values.ContainsKey(key))
            Values[key] = value;
        else
            Values.Add(key, value);
    }

    public void SetBool(string key, bool value)
    {
        SetKeyValuePair(key, value ? "1" : "0");
    }
}