using System;

namespace KartLibrary.Consts;

[AttributeUsage(AttributeTargets.Field)]
public class CountryCodeAttribute : Attribute
{
    public CountryCodeAttribute(string countryName)
    {
        CountryName = countryName;
    }

    public string CountryName { get; set; }
}