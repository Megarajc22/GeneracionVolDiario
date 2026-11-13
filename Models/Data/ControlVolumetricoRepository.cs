using Microsoft.Data.SqlClient;
using CovolGenerador.Models;

namespace CovolGenerador.Data;

public class ControlVolumetricoRepository
{
    private readonly string _connOperacion;
    private readonly string _connFacturacion;
    private const string ZonaHoraria = "-07:00"; // Sinaloa

    public ControlVolumetricoRepository(string connOperacion, string connFacturacion)
    {
        _connOperacion = connOperacion;
        _connFacturacion = connFacturacion;
    }

    public async Task<ControlesVolumetricosDiario> ObtenerReporteDiarioAsync(
        DateTime fechaDia,
        string rfcProveedorSoftware,
        string claveInstalacion,
        string permisoComercializadorDefault)
    {
        DateTime inicio = fechaDia.Date;
        DateTime fin = inicio.AddDays(1);
        DateTime corteActual = fin.AddSeconds(-1);
        DateTime corteAnterior = inicio.AddSeconds(-1);

        var reporte = new ControlesVolumetricosDiario
        {
            RfcProveedor = rfcProveedorSoftware,
            ClaveInstalacion = claveInstalacion,
            FechaYHoraCorte = FormatearFechaSat(corteActual)
        };

        await using var cnOp = new SqlConnection(_connOperacion);
        await cnOp.OpenAsync();

        // 1. Encabezado de Estación
        int estacionId = 0;
        const string sqlEstacion = @"
            SELECT TOP 1 EstacionID, RFC, RepresentanteRFC, TipoPermisoCRE, PermisoCRE,
                   Nombre, CAST(Latitud AS DECIMAL(10,7)) AS Lat, CAST(Longitud AS DECIMAL(10,7)) AS Lon,
                   (SELECT COUNT(*) FROM Tanque) AS NumTanques,
                   (SELECT COUNT(DISTINCT BombaID) FROM Bomba WHERE Activa = 1) AS NumDispensarios
            FROM Estacion";

        await using (var cmd = new SqlCommand(sqlEstacion, cnOp))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                estacionId = rd.GetInt32(0);
                reporte.RfcContribuyente = rd.GetString(1).Trim();
                reporte.RfcRepresentanteLegal = rd.GetString(2).Trim();
                reporte.Caracter.ModalidadPermiso = rd.GetString(3).Trim();
                reporte.Caracter.NumPermiso = rd.GetString(4).Trim();
                reporte.DescripcionInstalacion = rd.GetString(5).Trim();
                reporte.Geolocalizacion.Latitud = rd.GetDecimal(6);
                reporte.Geolocalizacion.Longitud = rd.GetDecimal(7);
                reporte.NumeroTanques = rd.GetInt32(8);
                reporte.NumeroDispensarios = rd.GetInt32(9);
            }
        }

        // 2. Catálogo de Productos Combustibles
        var productosMap = new Dictionary<int, (string Pemex, ProductoDiario Nodo)>();
        const string sqlProductos = @"
            SELECT ProductoID, Codigo, Nombre, CodigoA30, VolClaveSubProducto, VolOctanaje, VolConEtanol
            FROM Producto
            WHERE VolClaveProducto IS NOT NULL
            ORDER BY ProductoID";

        await using (var cmd = new SqlCommand(sqlProductos, cnOp))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync())
            {
                int prodId = rd.GetInt32(0);
                string codigoPemex = rd.GetString(1).Trim();
                string marca = rd.GetString(2).Trim();
                string claveProd = rd.GetString(3).Trim();
                int subProdNum = rd.GetInt32(4);
                int octanaje = rd.GetInt32(5);
                bool conEtanol = rd.GetBoolean(6);

                string subClave = claveProd == "PR07"
                    ? $"SP{15 + subProdNum}" // 1 -> SP16 (Regular), 2 -> SP17 (Supreme)
                    : "SP19";                // Diésel

                var prod = new ProductoDiario
                {
                    ClaveProducto = claveProd,
                    ClaveSubProducto = subClave,
                    MarcaComercial = marca,
                    Gasolina = claveProd == "PR07"
                        ? new GasolinaInfo { Octanaje = octanaje, ConCombustibleNoFosil = conEtanol ? "Si" : "No" }
                        : null,
                    Diesel = claveProd == "PR03"
                        ? new DieselInfo { ConCombustibleNoFosil = conEtanol ? "Si" : "No" }
                        : null
                };

                productosMap[prodId] = (codigoPemex, prod);
                reporte.Productos.Add(prod);
            }
        }

        // 3. Tanques, Existencias y Recepciones del Día
        const string sqlTanques = @"
            SELECT t.Tanque, t.Nombre, t.ProductoID, t.Capacidad, t.Operativa, t.Util, t.Fondaje, t.Minima,
                   t.VigenciaCalibracion, t.SistemaMedicion, t.Incertidumbre,
                   ISNULL(exAct.VolumenDisponible, 0) AS ExistenciaFinal,
                   ISNULL(exAnt.VolumenDisponible, 0) AS ExistenciaAnterior,
                   ISNULL(exAct.VolumenRecepcion, 0) AS AcumRecepcion,
                   ISNULL(exAct.VolumenExtraccion, 0) AS AcumEntrega
            FROM Tanque t
            LEFT JOIN V2ArchivoEXI exAct ON t.Tanque = exAct.Tanque AND exAct.FechaActual = @CorteActual
            LEFT JOIN V2ArchivoEXI exAnt ON t.Tanque = exAnt.Tanque AND exAnt.FechaActual = @CorteAnterior
            ORDER BY t.Tanque";

        await using (var cmd = new SqlCommand(sqlTanques, cnOp))
        {
            cmd.Parameters.AddWithValue("@CorteActual", corteActual);
            cmd.Parameters.AddWithValue("@CorteAnterior", corteAnterior);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                int numTanque = rd.GetInt32(0);
                int prodId = rd.GetInt32(2);
                if (!productosMap.ContainsKey(prodId)) continue;

                string vigencia = rd.IsDBNull(8) ? "2026-12-31" : rd.GetDateTime(8).ToString("yyyy-MM-dd");
                string sistMed = rd.IsDBNull(9) ? $"SME-STQ-EDS-{numTanque:D4}" : rd.GetString(9).Trim();
                decimal incertidumbre = rd.IsDBNull(10) ? 0.01m : Convert.ToDecimal(rd.GetDouble(10));

                var tanque = new TanqueDiario
                {
                    ClaveIdentificacionTanque = $"T{numTanque:D2}",
                    DescripcionTanque = rd.GetString(1).Trim(),
                    VigenciaCalibracion = vigencia,
                    CapacidadTotal = new VolumenUM { ValorNumerico = Convert.ToDecimal(rd.GetDouble(3)) },
                    CapacidadOperativa = new VolumenUM { ValorNumerico = Convert.ToDecimal(rd.GetDouble(4)) },
                    CapacidadUtil = new VolumenUM { ValorNumerico = Convert.ToDecimal(rd.GetDouble(5)) },
                    CapacidadFondaje = new VolumenUM { ValorNumerico = Convert.ToDecimal(rd.GetDouble(6)) },
                    VolumenMinimoOperacion = new VolumenUM { ValorNumerico = Convert.ToDecimal(rd.GetDouble(7)) },
                    MedicionTanque = new MedicionEquipo
                    {
                        SistemaMedicionTanque = sistMed,
                        DescripcionTanque = $"Telemedición Tanque {numTanque}",
                        VigenciaTanque = vigencia,
                        IncertidumbreTanque = incertidumbre
                    },
                    Existencias = new ExistenciasDiario
                    {
                        VolumenExistenciasAnterior = new VolumenUM { ValorNumerico = Math.Round(Convert.ToDecimal(rd.GetDouble(12)), 3) },
                        VolumenAcumOpsRecepcion = new VolumenUM { ValorNumerico = Math.Round(Convert.ToDecimal(rd.GetDouble(13)), 3) },
                        VolumenAcumOpsEntrega = new VolumenUM { ValorNumerico = Math.Round(Convert.ToDecimal(rd.GetDouble(14)), 3) },
                        VolumenExistencias = new VolumenUM { ValorNumerico = Math.Round(Convert.ToDecimal(rd.GetDouble(11)), 3) },
                        FechaYHoraEstaMedicion = FormatearFechaSat(corteActual),
                        FechaYHoraMedicionAnterior = FormatearFechaSat(corteAnterior)
                    }
                };

                await CargarRecepcionesTanqueAsync(cnOp, tanque, numTanque, inicio, fin);
                productosMap[prodId].Nodo.Tanques.Add(tanque);
            }
        }

        // 4. Dispensarios, Mangueras y Entregas del Día (Físicas + CFDIs timbrados)
        await CargarDispensariosYEntregasAsync(cnOp, productosMap, estacionId, inicio, fin, permisoComercializadorDefault);

        return reporte;
    }

    private async Task CargarRecepcionesTanqueAsync(
        SqlConnection cnOp, TanqueDiario tanque, int numTanque, DateTime inicio, DateTime fin)
    {
        // Enlace físico (DR) con factura de compra (DD) por FolioRelacion
        const string sqlRec = @"
            SELECT dr.VolumenRecepcion, dd.CFDI, dd.VolumenDocumentado, dd.PrecioCompra,
                   dd.FechaRecepcion, p.RFC, p.Nombre, p.PermisoProveedor
            FROM V2ArchivoREC dr
            LEFT JOIN V2ArchivoREC dd ON dr.FolioRelacion = dd.FolioRelacion AND dd.TipoRegistro = 'DD'
            LEFT JOIN ArchivoProveedor p ON dd.ArchivoProveedorID = p.ArchivoProveedorID
            WHERE dr.TipoRegistro = 'DR'
              AND dr.Tanque = @Tanque
              AND dr.FechaRecepcion >= @Inicio AND dr.FechaRecepcion < @Fin";

        await using var cmd = new SqlCommand(sqlRec, cnOp);
        cmd.Parameters.AddWithValue("@Tanque", numTanque);
        cmd.Parameters.AddWithValue("@Inicio", inicio);
        cmd.Parameters.AddWithValue("@Fin", fin);

        var operaciones = new List<OperacionNacional>();
        var uuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        decimal volTotal = 0;
        decimal impTotal = 0;
        int conteoFisico = 0;

        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            conteoFisico++;
            decimal volRec = Convert.ToDecimal(rd.GetDouble(0));
            volTotal += volRec;

            if (!rd.IsDBNull(1))
            {
                string uuid = rd.GetString(1).Trim();
                decimal volDoc = Convert.ToDecimal(rd.GetDouble(2));
                decimal precioCompra = Convert.ToDecimal(rd.GetDouble(3));

                // Guardia preventiva contra error de captura por m3 (ej. 18871 en lugar de 18.871)
                if (precioCompra > 500m)
                    precioCompra = Math.Round(precioCompra / 1000m, 4);

                DateTime fechaDoc = rd.GetDateTime(4);
                string rfcProv = rd.IsDBNull(5) ? "" : rd.GetString(5).Trim();
                string nomProv = rd.IsDBNull(6) ? "" : rd.GetString(6).Trim();
                string permProv = rd.IsDBNull(7) ? "" : rd.GetString(7).Trim();

                uuids.Add(uuid);
                impTotal += Math.Round(volDoc * precioCompra, 2);

                operaciones.Add(new OperacionNacional
                {
                    RfcClienteOProveedor = rfcProv,
                    NombreClienteOProveedor = nomProv,
                    PermisoProveedor = permProv,
                    Cfdi = new CfdiDetalle
                    {
                        Uuid = uuid,
                        TipoCfdi = "Ingreso",
                        PrecioCompra = Math.Round(precioCompra, 2),
                        PrecioDeVentaAlPublico = 0.00m,
                        PrecioVenta = 0.00m,
                        FechaYHoraTransaccion = FormatearFechaSat(fechaDoc),
                        VolumenDocumentado = new VolumenUM { ValorNumerico = Math.Round(volDoc, 3) }
                    }
                });
            }
        }

        tanque.Recepciones = new RecepcionesDiario
        {
            TotalRecepciones = conteoFisico,
            SumaVolumenRecepcion = new VolumenUM { ValorNumerico = Math.Round(volTotal, 3) },
            TotalDocumentos = uuids.Count,
            ImporteTotalRecepciones = Math.Round(impTotal, 2),
            Complemento = operaciones.Count > 0
                ? new ComplementoWrapper { Expendio = new ComplementoExpendio { Nacionales = operaciones } }
                : null
        };
    }

    private async Task CargarDispensariosYEntregasAsync(
        SqlConnection cnOp,
        Dictionary<int, (string Pemex, ProductoDiario Nodo)> productosMap,
        int estacionId,
        DateTime inicio,
        DateTime fin,
        string permisoComercializadorDefault)
    {
        // Obtener CFDIs timbrados del día desde Facturación y mapearlos a su TramaID (Bomba/Manguera)
        var cfdisPorManguera = await ObtenerCfdisDelDiaPorMangueraAsync(cnOp, estacionId, inicio, fin, permisoComercializadorDefault);

        // Obtener entregas físicas de V2ArchivoVTA por reloj calendario (00:00:00 a 23:59:59)
        const string sqlVta = @"
            SELECT b.BombaID, b.MangueraID, b.ProductoID, b.VigenciaCalibracion, b.Incertidumbre,
                   COUNT(v.Secuencia) AS TotalDespachos,
                   ISNULL(SUM(v.Volumen), 0) AS VolumenTotal,
                   ISNULL(SUM(v.Importe), 0) AS ImporteTotal
            FROM Bomba b
            INNER JOIN Producto p ON b.ProductoID = p.ProductoID
            LEFT JOIN V2ArchivoVTA v 
                ON b.BombaID = v.Dispensario 
               AND b.MangueraID = v.Manguera
               AND v.Fecha >= @Inicio AND v.Fecha < @Fin
            WHERE b.Activa = 1 AND p.VolClaveProducto IS NOT NULL
            GROUP BY b.BombaID, b.MangueraID, b.ProductoID, b.VigenciaCalibracion, b.Incertidumbre
            ORDER BY b.ProductoID, b.BombaID, b.MangueraID";

        await using var cmd = new SqlCommand(sqlVta, cnOp);
        cmd.Parameters.AddWithValue("@Inicio", inicio);
        cmd.Parameters.AddWithValue("@Fin", fin);

        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            int bombaId = rd.GetInt32(0);
            int mangueraId = rd.GetInt32(1);
            int prodId = rd.GetInt32(2);
            if (!productosMap.ContainsKey(prodId)) continue;

            string vigencia = rd.IsDBNull(3) ? "2026-12-31" : rd.GetDateTime(3).ToString("yyyy-MM-dd");
            decimal incertidumbre = rd.IsDBNull(4) ? 0.01m : Convert.ToDecimal(rd.GetDouble(4));
            int totalEntregas = rd.GetInt32(5);
            decimal volEntregado = Math.Round(Convert.ToDecimal(rd.GetDouble(6)), 3);
            decimal impEntregado = Math.Round(Convert.ToDecimal(rd.GetDouble(7)), 3);

            string claveDisp = $"DISP-{bombaId:D2}";
            var prodNodo = productosMap[prodId].Nodo;
            var disp = prodNodo.Dispensarios.FirstOrDefault(d => d.ClaveIdentificacionDispensario == claveDisp);
            if (disp == null)
            {
                disp = new DispensarioDiario { ClaveIdentificacionDispensario = claveDisp };
                prodNodo.Dispensarios.Add(disp);
            }

            cfdisPorManguera.TryGetValue((bombaId, mangueraId), out var opsManguera);
            opsManguera ??= new List<OperacionNacional>();

            int docsUnicos = opsManguera.Select(o => o.Cfdi.Uuid).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            disp.Mangueras.Add(new MangueraDiario
            {
                IdentificacionManguera = $"M{mangueraId:D2}",
                MedicionDispensario = new MedicionEquipo
                {
                    SistemaMedicionDispensario = $"SMD-DISP-{bombaId:D2}-M{mangueraId:D2}",
                    DescripcionDispensario = $"Dispensario {bombaId} Manguera {mangueraId}",
                    VigenciaDispensario = vigencia,
                    IncertidumbreDispensario = incertidumbre
                },
                Entregas = new EntregasDiario
                {
                    TotalEntregas = totalEntregas,
                    SumaVolumenEntregado = new VolumenUM { ValorNumerico = volEntregado },
                    TotalDocumentos = docsUnicos,
                    ImporteTotalEntregas = impEntregado,
                    Complemento = opsManguera.Count > 0
                        ? new ComplementoWrapper { Expendio = new ComplementoExpendio { Nacionales = opsManguera } }
                        : null
                }
            });
        }
    }

    private async Task<Dictionary<(int Bomba, int Manguera), List<OperacionNacional>>> ObtenerCfdisDelDiaPorMangueraAsync(
        SqlConnection cnOp,
        int estacionId,
        DateTime inicio,
        DateTime fin,
        string permisoComercializadorDefault)
    {
        var resultado = new Dictionary<(int, int), List<OperacionNacional>>();

        // 1. Obtener facturas del día desde BD Facturación cruzadas con TicketFactura
        var ticketsFacturados = new List<(int TicketId, string Rfc, string Nombre, string Uuid, string Tipo, decimal PrecioPub, decimal PrecioSub, DateTime FechaTimb, decimal Vol)>();

        await using (var cnFac = new SqlConnection(_connFacturacion))
        {
            await cnFac.OpenAsync();
            const string sqlFac = @"
                SELECT tf.TicketID, c.RFC, c.Nombre, f.UUID,
                       CASE WHEN f.NotaCredito = 1 THEN 'Egreso' ELSE 'Ingreso' END AS TipoCFDI,
                       ROUND(tf.Precio, 2) AS PrecioPublico,
                       ROUND(df.Importe / NULLIF(df.Cantidad, 0), 2) AS PrecioSinIva,
                       f.FechaTimbrado,
                       ROUND(tf.Volumen, 3) AS Volumen
                FROM Factura f
                INNER JOIN Cliente c ON f.ClienteID = c.ClienteID
                INNER JOIN TicketFactura tf ON f.FacturaID = tf.FacturaID
                INNER JOIN DetalleFactura df ON f.FacturaID = df.FacturaID AND tf.ProductoID = df.ProductoID
                WHERE f.EstacionID = @EstacionID
                  AND f.Cancelada = 0
                  AND f.UUID IS NOT NULL
                  AND f.Ejercicio = @Anio AND f.Periodo = @Mes AND f.Dia = @Dia";

            await using var cmd = new SqlCommand(sqlFac, cnFac);
            cmd.Parameters.AddWithValue("@EstacionID", estacionId);
            cmd.Parameters.AddWithValue("@Anio", inicio.Year);
            cmd.Parameters.AddWithValue("@Mes", inicio.Month);
            cmd.Parameters.AddWithValue("@Dia", inicio.Day);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                ticketsFacturados.Add((
                    rd.GetInt32(0),
                    rd.GetString(1).Trim(),
                    rd.GetString(2).Trim(),
                    rd.GetString(3).Trim(),
                    rd.GetString(4).Trim(),
                    Convert.ToDecimal(rd.GetDouble(5)),
                    Convert.ToDecimal(rd.GetDouble(6)),
                    rd.GetDateTime(7).AddHours(-1), // Ajuste hora Centro (-06) a Sinaloa (-07)
                    Convert.ToDecimal(rd.GetDouble(8))
                ));
            }
        }

        if (ticketsFacturados.Count == 0)
            return resultado;

        // 2. Mapear cada TicketID a su (BombaID, MangueraID) en BD Operación
        var mapaTrama = new Dictionary<int, (int Bomba, int Manguera)>();
        const string sqlTrama = @"
            SELECT TramaID, BombaID, MangueraID
            FROM Trama
            WHERE FechaCarga >= @Inicio AND FechaCarga < @Fin";

        await using (var cmd = new SqlCommand(sqlTrama, cnOp))
        {
            cmd.Parameters.AddWithValue("@Inicio", inicio.AddDays(-1)); // margen por si se facturó ticket de turno anterior
            cmd.Parameters.AddWithValue("@Fin", fin);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                mapaTrama[rd.GetInt32(0)] = (rd.GetInt32(1), rd.GetInt32(2));
            }
        }

        foreach (var t in ticketsFacturados)
        {
            if (!mapaTrama.TryGetValue(t.TicketId, out var ubicacion))
                continue;

            if (!resultado.ContainsKey(ubicacion))
                resultado[ubicacion] = new List<OperacionNacional>();

            resultado[ubicacion].Add(new OperacionNacional
            {
                RfcClienteOProveedor = t.Rfc,
                NombreClienteOProveedor = t.Nombre,
                PermisoProveedor = permisoComercializadorDefault,
                Cfdi = new CfdiDetalle
                {
                    Uuid = t.Uuid,
                    TipoCfdi = t.Tipo,
                    PrecioCompra = 0.00m,
                    PrecioDeVentaAlPublico = t.PrecioPub,
                    PrecioVenta = t.PrecioSub,
                    FechaYHoraTransaccion = FormatearFechaSat(t.FechaTimb),
                    VolumenDocumentado = new VolumenUM { ValorNumerico = t.Vol }
                }
            });
        }

        return resultado;
    }

    private static string FormatearFechaSat(DateTime fecha) =>
        $"{fecha:yyyy-MM-ddTHH:mm:ss}{ZonaHoraria}";
}
