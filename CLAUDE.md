# ARCO_2 — Contexto de desarrollo para Claude

## Qué es este proyecto

**ARCO_2** es una aplicación de escritorio en **VB.NET (.NET 4.7.2, Windows Forms)** para diseño estructural de elementos de concreto reforzado según la norma **NSR-10 (Colombia)**.

El flujo principal es:
1. El usuario importa un Excel exportado desde **ETABS** (modelos E17 o E23)
2. Selecciona combinaciones de carga para diseño
3. El programa verifica y diseña: vigas, columnas, muros, pilas, losas, escaleras
4. Se exportan resultados a Excel y se generan reportes

El usuario es el ingeniero estructural y desarrollador principal. El archivo de proyecto se guarda como `.esm` con `BinaryFormatter`.

---

## Estructura del repositorio

```
DiagramaInteracción/          ← proyecto VB.NET principal
  Clases/                     ← modelo de datos (serializable)
    Columnas/
    Muros/
    MurosNoEstructurales/
    cVigas.vb, cViga.vb, cFrame.vb ...
  Formularios/
    01_Pilas/
    02_Columnas/
    03_Losas/
    04_Escaleras/
    05_MurosNoEstructurales/
    06_Muros/
    09_Vigas/                 ← servicios de lógica (VigaService, DiagramaService, GeometryService)
    Form_00_PaginaPrincipal.vb
    Form_09_Vigas.vb
    Form_AyudaImportacion.vb
  Funciones/
    Funciones_00_Varias.vb    ← utilidades generales, AreaRefuerzo(), detección E17/E23
    Funciones_02_Columnas.vb  ← cálculo de DI, cortante, DistribuirBarrasConEsquinas
    Funciones_Muros.vb
    Logger.vb                 ← logging centralizado (archivo + Debug.Print)
  eNum/eNum.vb                ← todas las enumeraciones del proyecto
  Programa_ARCO.vbproj
```

---

## Convenciones críticas

### C/D vs D/C
- La empresa usa **C/D (Capacidad/Demanda)**. Un valor ≥ 1.0 significa que cumple.
- Internamente `F_Interaccion` almacena **D/C** (Bresler lineal). Al mostrar al usuario siempre convertir: `cd = 1 / F_Interaccion`.
- En el diagrama 3D biaxial se usa el criterio **elíptico** para colorear: `DC_e = sqrt((M3/φMn3)² + (M2/φMn2)²)`, que es geométricamente consistente con la superficie mostrada.

### Combinaciones de diseño
Cada módulo tiene su propia lista de combinaciones de diseño seleccionadas por el usuario:
- Columnas: `proyecto.Elementos.Columnas.ListA_Combinaciones_Design`
- Muros: `proyecto.Elementos.Muros.ListA_Combinaciones_Design`
- Vigas: seleccionadas en dialog independiente

**Regla importante:** Las combinaciones de diseño solo están disponibles DESPUÉS de que el usuario abre el diálogo de selección. Cualquier cálculo que dependa de ellas debe hacerse en `Obtencion_Macroparametros()` o en el `Button_Click` de guardar, **nunca** en el loop de carga de fuerzas.

### Áreas de barras (mm²)
`#2=32.3, #3=71.0, #4=129.4, #5=199.7, #6=284.3, #7=387.1, #8=509.7, #10=817.4`

Siempre usar `AreaRefuerzo("#N")` de `Funciones_00_Varias.vb`. Nunca hardcodear.

### Factor de cumplimiento muros
`Factor >= 0.9` → cumple (igual que vigas).

---

## ETABS: detección E17 vs E23

El programa soporta dos versiones de exportación de ETABS:

| Hoja | E17 | E23 |
|------|-----|-----|
| Joints | "Joint Coordinates" | "Objects and Elements - Joints" |
| Frames | "Connectivity - Frame" | "Objects and Elements - Frames" |
| Secciones | "Frame Assignments - Sections" | "Frame Assigns - Sect Prop" |
| Fuerzas vigas | "Beam Forces" | "Element Forces - Beams" |
| Fuerzas columnas | "Column Forces" | "Element Forces - Columns" |
| Fuerzas muros | "Pier Forces" | "Pier Forces" (misma, columnas distintas) |
| Diseño muros | "Shear Wall Pier Summary" | "Pier Dgn Sum" |

Usar `ObtenerHojasExcel()` y `ResolverNombreHoja()` de `Funciones_00_Varias.vb` para detección automática.

### Índices de fuerzas E23 (columnas Frame)
`"Element Forces - Columns"`: Story(0), Column(1), UniqueName(2), OutputCase(3), CaseType(4), StepType(5), StepNum(6), Station(7), **P(8), V2(9), V3(10), T(11), M2(12), M3(13)**

### Índices de fuerzas Pier
`"Pier Forces"`: Story(0), Pier(1), OutputCase(2), CaseType(3), StepType(4), Location(5), **P(6), V2(7), V3(8), T(9), M2(10), M3(11)**

---

## Módulo de Columnas

### Clases clave
- `cColumnas`: contenedor, flags `Elementos_Frame`/`Elementos_Pier`, `ListA_Combinaciones_Design`
- `Columna`: Name_Elemento, Lista_Tramos_Columnas
- `Tramo_Columna`: B_Plano, H_Plano, fc, Lista_Combinaciones, Lista_DI_M3_P_Phi, Lista_DI_M2_P_Phi, Lista_DI_M3_Phi, Lista_DI_M2_Phi, F_Interaccion (D/C Bresler), Combo_Gobernante_DI

### Formularios
- `Form_02_PagColumnas.vb`: importación (Button2_Click = Calcular), mixto Frame+Pier
- `Form_02_00_PagInfoColumnas.vb`: ingreso de refuerzo y verificación
- `Form_02_02_DiagramaColumna.vb`: diagrama de interacción 2D + tabla de combinaciones
- `Form_02_03_DI_3D.vb`: diagrama de interacción biaxial 3D (surface P-M3-M2, rotación interactiva)
- `Form_02_01_ResultadosColumnas.vb`: resultados + export Excel

### Funciones clave (`Funciones_02_Columnas.vb`)
- `FuncionDiagramaColumna(tramo, fy, Es, estacion, Optional combosDiseno)`: genera diagrama DI. Acepta lista de combos de diseño para filtrar.
- `DistribuirBarrasConEsquinas(B, H, N)`: distribuye N barras garantizando 4 en esquinas + intermedias proporcionales por cara. Usar esta (no `DistribuirBarrasPerimetro`).
- `InterpolarMnEnPu(listaP, listaMn, pVal)`: interpola capacidad φMn dado un Pu.

### Bug resuelto julio 2026
- Frame+Pier: `Columna = New Columna()` al inicio del loop (era `Public Shared`, acumulaba entre calls)
- Índices forces E23: P=8, V2=9 (antes estaban off by 1)

---

## Módulo de Muros

### Clases clave
- `Muro`: Label, Lista_Secciones, Ref_Modificado_Muros (bool: tiene refuerzo), ListA_Combinaciones_Design
- `SeccionMuro`: S_Patron (bool: es sección maestra/principal), Lista_Combinaciones, Vu
- `ElementoBorde`: EB_I_Top/Bot, EB_D_Top/Bot, cada uno con L_EB, Barras_L (Barras_2..Barras_10), RefH

### Formularios
- `Form_06_PagMuros.vb`: importación y cálculo, `Obtencion_Macroparametros()` — aquí se calcula Vu y otros parámetros post-selección de combos
- `Form_06_00_PagInfoMuros.vb`: ingreso de refuerzo por sección
- `Form_06_01_ResultadosMuros.vb`: resultados
- `Form_DiagramaInteraccionMuro.vb`: diagrama P-M interactivo
- `Form_SeccionMuroViewer.vb`: sección transversal GDI+
- `Form_Reporte_Resumen_Muros.vb`: 4 tabs + export Excel (ClosedXML)
- `Form_Reporte_Ejecutivo_Muros.vb`: resumen ejecutivo plano, checkbox "solo con observaciones"

### Índices DataGridView en `Form_06_00_PagInfoMuros.vb` (`Tabla_Info_EBorde`)
El mapeo de celdas es crítico — Button4_Click (guardar) y SelectedIndexChanged (mostrar) deben coincidir exactamente. Los bloques EB_I ocupan columnas 7-19, EB_D ocupa columnas 21-33.

### Filtro combo `C_Lista_Secciones_Principales`
Solo mostrar muros que tienen `Ref_Modificado_Muros = True` **Y** al menos una sección con `S_Patron = True`.

### Vu en muros
Se calcula en `Obtencion_Macroparametros()` usando `ListA_Combinaciones_Design`:
```vb
sec.Vu = combsD.Max(Function(c) Math.Abs(c.V2))
```

---

## Módulo de Vigas

### Servicios (`Formularios/09_Vigas/`)
- `VigaService.vb`: GenerarVigas, OrdenarFramesViga, CalcularEnvolventesVigas, DesignVigas, AsignarRefuerzoTransversalAutomatico, CalcularCortantePlastico
- `DiagramaService.vb`: diagramas y planta interactiva
- `GeometryService.vb`: geometría de frames

### Formulario (`Form_09_Vigas.vb`)
Tablas: Tabla_Demandas, Ref_Superior, Ref_Inferior, Ref_Transversal, Tabla_Resultados_Flexion, Tabla_Resultados_Cortante.

### Fórmulas clave (NSR-10)
- ρ_min = máx(1.4/fy, 0.25·√fc/fy)
- Vc = 0.17·√fc·b·d·1000 [kN]
- Vs = Av·fy·d/s/1000 [kN]
- φVn = 0.75·(Vc+Vs)

### Refuerzo transversal automático
- Zonas Izq/Der: barra #3, sep = d/4, numEstribos = ⌈2H/(d/4)⌉
- Zona Centro: barra #3, 2 ramas, sep = d/2

### Cortante Plástico (NSR-10 C.21.5.4)
- fy_dis = fy × fy_factor (1.0 DMO, 1.25 DES)
- Ve_A = (Mn_neg_izq + Mn_pos_der) / Ln

---

## Infraestructura transversal

### Logger (`Funciones/Logger.vb`)
```vb
Logger.Info("mensaje")
Logger.Warning("mensaje")
Logger.Error("mensaje")
Logger.Critical("mensaje")
```
Escribe a Debug.Print y a archivo en `[AppDir]\Logs\ARCO_yyyy-MM-dd.log`. Thread-safe con SyncLock.

### Excel Interop — patrón obligatorio
```vb
Dim xlApp As Excel.Application = Nothing
Try
    xlApp = New Excel.Application()
    ' ... trabajo ...
Catch ex As Exception
    Logger.Error(ex.Message)
Finally
    If xlApp IsNot Nothing Then
        xlApp.Quit()
        Marshal.ReleaseComObject(xlApp)
        GC.Collect()
    End If
End Try
```
Siempre usar `Imports System.Runtime.InteropServices`.

### BinaryFormatter — compatibilidad
Al agregar campos nuevos a clases serializadas usar `<OptionalField>` + `<OnDeserialized>` para inicializar nulls. Esto evita errores al abrir proyectos guardados con versiones anteriores.

### Form_AyudaImportacion
Acceso desde el menú "? Tablas ETABS" en cada módulo. Muestra qué hojas ETABS necesita cada módulo y sus nombres en E17/E23.

---

## Auditoría de calidad — pendiente

- **TryParse**: Form_03_Losas (~25), Form_04_Escaleras (~15), Form_05_MurosNoEstructurales (~12), Form_06_PagMuros (~8). Estrategia: Try/Catch FormatException a nivel del sub de cálculo, no por campo.
- **Nomenclatura inconsistente**: baja prioridad.

---

## Palabras reservadas VB.NET — errores frecuentes

- `step` es palabra reservada. Usar `stepVal` o similar.
- Los enums son tipos, no strings: `eNumeradores.eDireccion.Y`, no `"Y"`.
- `RefuerzoSimple` tiene `.Coordenada_X` y `.Coordenada_Y`, no `.X` o `.Y`.
