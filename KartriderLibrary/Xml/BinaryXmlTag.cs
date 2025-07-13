using KartLibrary.Text;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace KartLibrary.Xml;

public class BinaryXmlTag : DynamicObject
{
    public dynamic? GetAttribute(string Attribute)
    {
        if (!Attributes.ContainsKey(Attribute))
            return null;
        return new BinaryXmlAttributeValue(Attributes[Attribute]);
    }

    public void SetAttribute(string name, string value)
    {
        if (!Attributes.ContainsKey(name))
            _attributes.Add(name, value);
        else
            _attributes[name] = value;
    }


    public override string ToString()
    {
        var tf = new TextFormater
        {
            LevelDelta = 4
        };
        ToString(ref tf, 0);
        return tf.StartFormat();
    }

    public void ToString(ref TextFormater formater, int nowLevel)
    {
        var HaveText = Text != null && Text != "";
        var HaveAttributes = Attributes.Count > 0;
        var HaveSubTag = Children.Count > 0;
        var Start = "";
        var Att = "";
        var End = "";
        var addition = "";
        var OneLine = true;
        if (HaveText || HaveSubTag)
        {
            End = $"</{Name}>";
            OneLine = !HaveSubTag;
        }
        else
        {
            End = "";
            OneLine = true;
            addition = "/";
        }

        if (HaveAttributes)
        {
            var attFormat = new List<string>();
            foreach (var KeyPair in Attributes) attFormat.Add($"{KeyPair.Key}=\"{KeyPair.Value}\"");
            Att = $" {string.Join(" ", attFormat)}";
        }

        Start = $"<{Name}{Att}{addition}>";
        if (OneLine)
        {
            formater.AddString(nowLevel, TextAlign.Top, $"{Start}{Text ?? ""}{End}");
        }
        else
        {
            formater.AddString(nowLevel, TextAlign.Top, $"{Start}{Text ?? ""}");
            foreach (var sub in Children) sub.ToString(ref formater, nowLevel + 1);
            formater.AddString(nowLevel, TextAlign.Top, End);
        }
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var attributeName = binder.Name;
        if (!_attributes.ContainsKey(attributeName))
        {
            result = null;
            return false;
        }

        var attributeValue = _attributes[attributeName];
        result = new BinaryXmlAttributeValue(attributeValue);
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var attributeName = binder.Name;
        var attributeValue = value?.ToString() ?? "";
        SetAttribute(attributeName, attributeValue);
        return true;
    }

    public static explicit operator BinaryXmlTag(XmlNode node)
    {
        if (node.NodeType != XmlNodeType.Element)
            throw new InvalidOperationException();
        var output = new BinaryXmlTag();
        output.Name = node.Name;
        output.Text = node.InnerText;
        foreach (XmlAttribute xmlAttribute in node.Attributes)
            output._attributes.Add(xmlAttribute.Name, xmlAttribute.Value);
        foreach (XmlNode xmlNode in node.ChildNodes)
            if (xmlNode.NodeType == XmlNodeType.Element)
                output.Children.Add((BinaryXmlTag)xmlNode);

        return output;
    }

    #region Members

    private readonly Dictionary<string, string> _attributes;
    private readonly List<BinaryXmlTag> _children;

    #endregion

    #region Properties

    public string Name { get; set; }

    public string Text { get; set; }

    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    public IList<BinaryXmlTag> Children => _children;

    public IEnumerable<BinaryXmlTag> this[string t] => _children.Where(x => x.Name == t);

    #endregion

    #region Constructor

    public BinaryXmlTag()
    {
        _children = new List<BinaryXmlTag>();
        _attributes = new Dictionary<string, string>();
        Name = "";
        Text = "";
    }

    public BinaryXmlTag(string name) : this()
    {
        Name = name;
    }

    public BinaryXmlTag(string name, string text) : this()
    {
        Name = name;
        Text = text;
    }

    public BinaryXmlTag(string name, params BinaryXmlTag[] children) : this()
    {
        Name = name;
        _children.AddRange(children);
    }

    #endregion
}

public sealed class BinaryXmlAttributeValue
{
    internal BinaryXmlAttributeValue(string baseValue)
    {
        BaseValue = baseValue;
    }

    public string BaseValue { get; }

    public static implicit operator string(BinaryXmlAttributeValue value)
    {
        return value.BaseValue;
    }

    public static implicit operator sbyte(BinaryXmlAttributeValue value)
    {
        return sbyte.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator short(BinaryXmlAttributeValue value)
    {
        return short.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator int(BinaryXmlAttributeValue value)
    {
        return int.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator long(BinaryXmlAttributeValue value)
    {
        return long.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator Int128(BinaryXmlAttributeValue value)
    {
        return Int128.Parse(value.BaseValue, NumberStyles.Number);
    }

    public static implicit operator byte(BinaryXmlAttributeValue value)
    {
        return byte.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator ushort(BinaryXmlAttributeValue value)
    {
        return ushort.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator uint(BinaryXmlAttributeValue value)
    {
        return uint.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator ulong(BinaryXmlAttributeValue value)
    {
        return ulong.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator UInt128(BinaryXmlAttributeValue value)
    {
        return UInt128.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator BigInteger(BinaryXmlAttributeValue value)
    {
        return BigInteger.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator float(BinaryXmlAttributeValue value)
    {
        return float.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator double(BinaryXmlAttributeValue value)
    {
        return double.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator decimal(BinaryXmlAttributeValue value)
    {
        return decimal.Parse(value.BaseValue, NumberStyles.Any);
    }

    public static implicit operator bool(BinaryXmlAttributeValue value)
    {
        return bool.Parse(value.BaseValue.ToLower());
    }

    public static implicit operator DateTime(BinaryXmlAttributeValue value)
    {
        return DateTime.Parse(value.BaseValue);
    }
}