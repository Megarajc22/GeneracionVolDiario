using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CovolGenerador.Models;

namespace CovolGenerador.Services;

public class GeneradorCovolDiarioService
{
    public string GenerarArchivoXml(ControlesVolumetricosDiario reporte, DateTime fechaCorte, string carpetaSalida)
    {
        Directory.CreateDirectory(carpetaSalida);

        // Nomenclatura oficial SAT para envío Diario:
        // D_<UUID>_<RfcContribuyente>_<RfcProveedor>_<YYYY-MM-DD>_<ClaveInstalacion>_EXO_XML.xml
        string uuidArchivo = Guid.NewGuid().ToString().ToUpperInvariant();
        string nombreArchivo = $"D_{uuidArchivo}_{reporte.RfcContribuyente}_{reporte.RfcProveedor}_{fechaCorte:yyyy-MM-dd}_{reporte.ClaveInstalacion}_EXO_XML.xml";
        string rutaCompleta = Path.Combine(carpetaSalida, nombreArchivo);

        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("Covol", "https://repositorio.cloudb.sat.gob.mx/Covol/xml/Diarios");
        namespaces.Add("exp", "Complemento_Expendio");
        namespaces.Add("ns", "Complemento_Expendio");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), // UTF-8 sin BOM exigido por SAT
            Indent = true,
            IndentChars = "  "
        };

        var serializer = new XmlSerializer(typeof(ControlesVolumetricosDiario));

        using var writer = XmlWriter.Create(rutaCompleta, settings);
        serializer.Serialize(writer, reporte, namespaces);

        return rutaCompleta;
    }
}
