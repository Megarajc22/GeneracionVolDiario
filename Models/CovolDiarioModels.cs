using System.Xml.Serialization;

namespace CovolGenerador.Models;

[XmlRoot("ControlesVolumetricos", Namespace = "https://repositorio.cloudb.sat.gob.mx/Covol/xml/Diarios")]
public class ControlesVolumetricosDiario
{
    [XmlElement("Version")]
    public string Version { get; set; } = "1.0";

    [XmlElement("RfcContribuyente")]
    public string RfcContribuyente { get; set; } = string.Empty;

    [XmlElement("RfcRepresentanteLegal")]
    public string RfcRepresentanteLegal { get; set; } = string.Empty;

    [XmlElement("RfcProveedor")]
    public string RfcProveedor { get; set; } = string.Empty;

    [XmlElement("Caracter")]
    public Caracter Caracter { get; set; } = new();

    [XmlElement("ClaveInstalacion")]
    public string ClaveInstalacion { get; set; } = string.Empty;

    [XmlElement("DescripcionInstalacion")]
    public string DescripcionInstalacion { get; set; } = string.Empty;

    [XmlElement("Geolocalizacion")]
    public Geolocalizacion Geolocalizacion { get; set; } = new();

    [XmlElement("NumeroPozos")]
    public int NumeroPozos { get; set; } = 0;

    [XmlElement("NumeroTanques")]
    public int NumeroTanques { get; set; }

    [XmlElement("NumeroDuctosEntradaSalida")]
    public int NumeroDuctosEntradaSalida { get; set; } = 0;

    [XmlElement("NumeroDuctosTransporteDistribucion")]
    public int NumeroDuctosTransporteDistribucion { get; set; } = 0;

    [XmlElement("NumeroDispensarios")]
    public int NumeroDispensarios { get; set; }

    [XmlElement("FechaYHoraCorte")]
    public string FechaYHoraCorte { get; set; } = string.Empty;

    [XmlElement("PRODUCTO")]
    public List<ProductoDiario> Productos { get; set; } = new();
}

public class Caracter
{
    [XmlElement("TipoCaracter")]
    public string TipoCaracter { get; set; } = "permisionario";

    [XmlElement("ModalidadPermiso")]
    public string ModalidadPermiso { get; set; } = "PER2";

    [XmlElement("NumPermiso")]
    public string NumPermiso { get; set; } = string.Empty;
}

public class Geolocalizacion
{
    [XmlElement("GeolocalizacionLatitud")]
    public decimal Latitud { get; set; }

    [XmlElement("GeolocalizacionLongitud")]
    public decimal Longitud { get; set; }
}

public class ProductoDiario
{
    [XmlElement("ClaveProducto")]
    public string ClaveProducto { get; set; } = string.Empty;

    [XmlElement("ClaveSubProducto")]
    public string ClaveSubProducto { get; set; } = string.Empty;

    // Se serializa solo cuando ClaveProducto == "PR07" (Gasolinas)
    [XmlElement("Gasolina")]
    public GasolinaInfo? Gasolina { get; set; }

    // Se serializa solo cuando ClaveProducto == "PR03" (Diésel)
    [XmlElement("Diesel")]
    public DieselInfo? Diesel { get; set; }

    [XmlElement("MarcaComercial")]
    public string MarcaComercial { get; set; } = string.Empty;

    [XmlElement("TANQUE")]
    public List<TanqueDiario> Tanques { get; set; } = new();

    [XmlElement("DISPENSARIO")]
    public List<DispensarioDiario> Dispensarios { get; set; } = new();
}

public class GasolinaInfo
{
    [XmlElement("ComposOctanajeGasolina")]
    public int Octanaje { get; set; }

    [XmlElement("GasolinaConCombustibleNoFosil")]
    public string ConCombustibleNoFosil { get; set; } = "No";
}

public class DieselInfo
{
    [XmlElement("DieselConCombustibleNoFosil")]
    public string ConCombustibleNoFosil { get; set; } = "No";
}

public class TanqueDiario
{
    [XmlElement("ClaveIdentificacionTanque")]
    public string ClaveIdentificacionTanque { get; set; } = string.Empty;

    [XmlElement("LocalizacionODescripcionTanque")]
    public string DescripcionTanque { get; set; } = string.Empty;

    [XmlElement("VigenciaCalibracionTanque")]
    public string VigenciaCalibracion { get; set; } = string.Empty;

    [XmlElement("CapacidadTotalTanque")]
    public VolumenUM CapacidadTotal { get; set; } = new();

    [XmlElement("CapacidadOperativaTanque")]
    public VolumenUM CapacidadOperativa { get; set; } = new();

    [XmlElement("CapacidadUtilTanque")]
    public VolumenUM CapacidadUtil { get; set; } = new();

    [XmlElement("CapacidadFondajeTanque")]
    public VolumenUM CapacidadFondaje { get; set; } = new();

    [XmlElement("VolumenMinimoOperacion")]
    public VolumenUM VolumenMinimoOperacion { get; set; } = new();

    [XmlElement("EstadoTanque")]
    public string EstadoTanque { get; set; } = "O";

    [XmlElement("MedicionTanque")]
    public MedicionEquipo MedicionTanque { get; set; } = new();

    [XmlElement("EXISTENCIAS")]
    public ExistenciasDiario Existencias { get; set; } = new();

    [XmlElement("RECEPCIONES")]
    public RecepcionesDiario Recepciones { get; set; } = new();
}

public class ExistenciasDiario
{
    [XmlElement("VolumenExistenciasAnterior")]
    public VolumenUM VolumenExistenciasAnterior { get; set; } = new();

    [XmlElement("VolumenAcumOpsRecepcion")]
    public VolumenUM VolumenAcumOpsRecepcion { get; set; } = new();

    [XmlElement("VolumenAcumOpsEntrega")]
    public VolumenUM VolumenAcumOpsEntrega { get; set; } = new();

    [XmlElement("VolumenExistencias")]
    public VolumenUM VolumenExistencias { get; set; } = new();

    [XmlElement("FechaYHoraEstaMedicion")]
    public string FechaYHoraEstaMedicion { get; set; } = string.Empty;

    [XmlElement("FechaYHoraMedicionAnterior")]
    public string FechaYHoraMedicionAnterior { get; set; } = string.Empty;
}

public class RecepcionesDiario
{
    [XmlElement("TotalRecepciones")]
    public int TotalRecepciones { get; set; }

    [XmlElement("SumaVolumenRecepcion")]
    public VolumenUM SumaVolumenRecepcion { get; set; } = new();

    [XmlElement("TotalDocumentos")]
    public int TotalDocumentos { get; set; }

    [XmlElement("ImporteTotalRecepciones")]
    public decimal ImporteTotalRecepciones { get; set; }

    [XmlElement("Complemento")]
    public ComplementoWrapper? Complemento { get; set; }
}

public class DispensarioDiario
{
    [XmlElement("ClaveIdentificacionDispensario")]
    public string ClaveIdentificacionDispensario { get; set; } = string.Empty;

    [XmlElement("MANGUERA")]
    public List<MangueraDiario> Mangueras { get; set; } = new();
}

public class MangueraDiario
{
    [XmlElement("IdentificacionManguera")]
    public string IdentificacionManguera { get; set; } = string.Empty;

    [XmlElement("MedicionDispensario")]
    public MedicionEquipo MedicionDispensario { get; set; } = new();

    [XmlElement("ENTREGAS")]
    public EntregasDiario Entregas { get; set; } = new();
}

public class EntregasDiario
{
    [XmlElement("TotalEntregas")]
    public int TotalEntregas { get; set; }

    [XmlElement("SumaVolumenEntregado")]
    public VolumenUM SumaVolumenEntregado { get; set; } = new();

    [XmlElement("TotalDocumentos")]
    public int TotalDocumentos { get; set; }

    [XmlElement("ImporteTotalEntregas")]
    public decimal ImporteTotalEntregas { get; set; }

    [XmlElement("Complemento")]
    public ComplementoWrapper? Complemento { get; set; }
}

public class MedicionEquipo
{
    [XmlElement("SistemaMedicionTanque")]
    public string? SistemaMedicionTanque { get; set; }

    [XmlElement("SistemaMedicionDispensario")]
    public string? SistemaMedicionDispensario { get; set; }

    [XmlElement("LocalizODescripSistMedicionTanque")]
    public string? DescripcionTanque { get; set; }

    [XmlElement("LocalizODescripSistMedicionDispensario")]
    public string? DescripcionDispensario { get; set; }

    [XmlElement("VigenciaCalibracionSistMedicionTanque")]
    public string? VigenciaTanque { get; set; }

    [XmlElement("VigenciaCalibracionSistMedicionDispensario")]
    public string? VigenciaDispensario { get; set; }

    [XmlElement("IncertidumbreMedicionSistMedicionTanque")]
    public decimal? IncertidumbreTanque { get; set; }

    [XmlElement("IncertidumbreMedicionSistMedicionDispensario")]
    public decimal? IncertidumbreDispensario { get; set; }
}

public class VolumenUM
{
    [XmlElement("ValorNumerico")]
    public decimal ValorNumerico { get; set; }

    [XmlElement("UM")]
    public string UM { get; set; } = "UM03"; // UM03 = Litros
}

public class ComplementoWrapper
{
    [XmlElement("Complemento_Expendio", Namespace = "Complemento_Expendio")]
    public ComplementoExpendio Expendio { get; set; } = new();
}

public class ComplementoExpendio
{
    [XmlElement("NACIONAL", Namespace = "Complemento_Expendio")]
    public List<OperacionNacional> Nacionales { get; set; } = new();
}

public class OperacionNacional
{
    [XmlElement("RfcClienteOProveedor", Namespace = "Complemento_Expendio")]
    public string RfcClienteOProveedor { get; set; } = string.Empty;

    [XmlElement("NombreClienteOProveedor", Namespace = "Complemento_Expendio")]
    public string NombreClienteOProveedor { get; set; } = string.Empty;

    [XmlElement("PermisoProveedor", Namespace = "Complemento_Expendio")]
    public string PermisoProveedor { get; set; } = string.Empty;

    [XmlElement("CFDIs", Namespace = "Complemento_Expendio")]
    public CfdiDetalle Cfdi { get; set; } = new();
}

public class CfdiDetalle
{
    [XmlElement("CFDI", Namespace = "Complemento_Expendio")]
    public string Uuid { get; set; } = string.Empty;

    [XmlElement("TipoCFDI", Namespace = "Complemento_Expendio")]
    public string TipoCfdi { get; set; } = "Ingreso";

    [XmlElement("PrecioCompra", Namespace = "Complemento_Expendio")]
    public decimal PrecioCompra { get; set; }

    [XmlElement("PrecioDeVentaAlPublico", Namespace = "Complemento_Expendio")]
    public decimal PrecioDeVentaAlPublico { get; set; }

    [XmlElement("PrecioVenta", Namespace = "Complemento_Expendio")]
    public decimal PrecioVenta { get; set; }

    [XmlElement("FechaYHoraTransaccion", Namespace = "Complemento_Expendio")]
    public string FechaYHoraTransaccion { get; set; } = string.Empty;

    [XmlElement("VolumenDocumentado", Namespace = "Complemento_Expendio")]
    public VolumenUM VolumenDocumentado { get; set; } = new();
}
