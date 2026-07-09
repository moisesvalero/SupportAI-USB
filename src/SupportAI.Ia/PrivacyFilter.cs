using System.Text.RegularExpressions;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public static class PrivacyFilter
{
    public static Diagnostico Anonymize(Diagnostico diag)
    {
        var hw = diag.Hardware;
        if (hw is not null)
        {
            hw = hw with
            {
                BIOS = hw.BIOS is { } b ? b with
                {
                    Fabricante = AnonymizeSerial(b.Fabricante),
                    Version = AnonymizeSerial(b.Version)
                } : null,
                SO = hw.SO is { } s ? s with
                {
                    Caption = s.Caption.Replace(Environment.UserName, "[USER]"),
                    Version = s.Version?.Split('.').FirstOrDefault() ?? ""
                } : null
            };
        }

        var red = diag.Red;
        if (red is not null)
        {
            red = red with
            {
                DNS = red.DNS?.Contains('.') == true ? "[DNS_SERVER]" : red.DNS,
                Gateway = red.Gateway?.Contains('.') == true ? "[GATEWAY]" : red.Gateway,
                Adaptadores = red.Adaptadores.Select(a => a with
                {
                    IP = a.IP?.Contains('.') == true ? "[IP_ADDRESS]" : a.IP,
                    Nombre = AnonymizeSerial(a.Nombre)
                }).ToList()
            };
        }

        return diag with { Hardware = hw, Red = red };
    }

    private static string AnonymizeSerial(string value)
    {
        return Regex.Replace(value, @"[A-Z0-9]{8,}", "[REDACTED]");
    }
}
