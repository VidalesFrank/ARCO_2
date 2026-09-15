---
name: Patrón estándar de formularios de reporte
description: Estructura code-only validada para todos los formularios Form_Reporte_* del proyecto
type: project
---

Todos los formularios de reporte de ARCO_2 siguen este patrón (validado en Nervios, VigasFundacion):

**Estructura general:**
- `Public Class Form_Reporte_XXX : Inherits Form` — code-only, sin Designer
- Propiedad pública para recibir datos: `Public Property Datos As cXxx`
- `Sub New()` construye toda la UI: TabControl (Dock=Fill) + Panel inferior (Dock=Bottom, Height=54)
- `AddHandler Me.Load, AddressOf Form_Load` en el constructor

**TabControl:**
- Tabs con prefijo/sufijo de espacios en el texto: `"  Revisión Flexión  "`
- BackColor = White, UseVisualStyleBackColor = False en cada TabPage
- DgvXxx.Dock = Fill dentro de cada TabPage

**Barra inferior (Panel Bottom, Height=54, BackColor=#F5F5F5):**
- CheckBox `_chkSoloObs` a la izquierda (Location X=10, Y=16)
- Botón "Actualizar" en gris oscuro (#575757), Size(120,36), Location(350,9)
- Botón "Exportar a Excel" en verde (#158246 ≈ RGB 21,130,70), Size(160,36), Location(480,9)
- Ambos botones: FlatStyle=Flat, FlatAppearance.BorderSize=0, Cursor=Hand

**Paleta de colores (siempre los mismos):**
- Encabezado grid: #575757 fondo, blanco texto
- OK: fondo #C6EFCE, texto #006100
- Mal: fondo #FFC7CE, texto #9C0006
- Fila alternada: #F8F8F8
- XlFilaPar (ClosedXML): #F8F8F8

**Filtros:**
- Checkbox afecta AMBOS tabs simultáneamente (llama CargarTodo())
- Umbral de cumplimiento: C/D < 0.9 = no cumple (aplica a vigas, muros, nervios, vigas de fundación)

**Exportación Excel (ClosedXML):**
- Una hoja por tab: `wb.Worksheets.Add("Revisión Flexión")` / `"Revisión Cortante"`
- Encabezados fila 1 con helper `EscribirEncabezados(ws, 1, enc())`
- Datos desde fila 2
- `EscribirFactor()` para columnas C/D (coloreado automático, NumberFormat "0.00")
- `EscribirEstado()` para columna Estado (OK/Revisar)
- `AjustarColumnas()` + `AgregarBordesTabla()` + `ws.SheetView.FreezeRows(1)` al final
- `Logger.Error()` en el Catch del exportador

**Why:** Patrón consolidado a partir de Form_Reporte_Resumen_Nervios.vb como referencia principal.

**How to apply:** Copiar helpers privados (EstilarGrid, AgregarColumna, AsignarCD, EscribirEncabezados, EscribirFactor, EscribirEstado, EstilarFilaDatos, AgregarBordesTabla, AjustarColumnas) entre formularios de reporte — son idénticos en todos los módulos.
