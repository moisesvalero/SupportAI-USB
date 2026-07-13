using System.Text.RegularExpressions;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public static class PrivacyFilter
{
    private static readonly Regex UsersPathRegex = new(@"c:\\users\\[^\\]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SerialRegex = new(@"[A-Za-z0-9]{8,}", RegexOptions.Compiled);
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

        var salud = diag.Salud;
        if (salud is not null)
        {
            salud = salud with
            {
                ProgramasInicio = salud.ProgramasInicio.Select(p => p with
                {
                    Nombre = CleanPii(p.Nombre),
                    Comando = CleanPii(p.Comando),
                    Ubicacion = CleanPii(p.Ubicacion)
                }).ToList()
            };
        }

        var windows = diag.Windows;
        if (windows is not null)
        {
            windows = windows with
            {
                EventosCriticos = windows.EventosCriticos.Select(e => e with
                {
                    Fuente = CleanPii(e.Fuente),
                    Mensaje = CleanPii(e.Mensaje)
                }).ToList()
            };
        }

        return diag with { Hardware = hw, Red = red, Salud = salud, Windows = windows };
    }

    private static string CleanPii(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // 1. Rutas de perfiles de usuario: C:\Users\<usuario>... -> C:\Users\[USER]...
        text = UsersPathRegex.Replace(text, @"C:\Users\[USER]");

        // 2. Nombre del usuario actual suelto
        text = text.Replace(Environment.UserName, "[USER]", StringComparison.OrdinalIgnoreCase);

        // 3. Nombre del equipo actual suelto
        text = text.Replace(Environment.MachineName, "[COMPUTER]", StringComparison.OrdinalIgnoreCase);

        // 4. Dominio del usuario actual suelto
        var domain = Environment.UserDomainName;
        if (!string.IsNullOrEmpty(domain) &&
            !domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
            !domain.Equals("WORKGROUP", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Replace(domain, "[DOMAIN]", StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    private static string AnonymizeSerial(string value)
    {
        return SerialRegex.Replace(value, "[REDACTED]");
    }
}
