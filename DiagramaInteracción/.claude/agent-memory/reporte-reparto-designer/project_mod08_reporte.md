---
name: Módulo 08 Vigas de Fundación — Reporte
description: Campos usados, estructura del formulario de reporte y dónde instanciarlo
type: project
---

Formulario creado: `Formularios/08_VigasFundacion/Form_Reporte_VigasFundacion.vb`

**Clase de datos:** `cVigasFundacion` (contenedor) con `.Elementos As List(Of cVigaFundacion)`.
Filtro obligatorio: solo elementos donde `vf.Calculado = True`.

**Campos usados en el reporte:**
- Identificación: `Nombre`, `NombrePlano` → `NombreViga = If(NombrePlano <> "", NombrePlano, Nombre)`
- Sección: `B`, `H`, `L` (metros) → label `"{B*100:F0}x{H*100:F0}/{L:F2}m"`
- Flexión: `As_Prov_Sup`, `As_Prov_Inf`, `As_Req_Sup`, `As_Req_Inf`, `As_Min`, `CD_Flex_Sup`, `CD_Flex_Inf`
  - As Req a mostrar = `Math.Max(As_Req_Sup/Inf, As_Min)`
- Cortante: `Vu`, `PhiVc`, `PhiVs`, `PhiVn`, `CD_Cortante`
- Estado: `Cumple` (Boolean), `Calculado` (Boolean)

**Observaciones flexión:** "En zona sup por M(-)" si CD_Flex_Sup < 0.9; "En zona inf por M(+)" si CD_Flex_Inf < 0.9.

**Umbral de cumplimiento:** C/D ≥ 0.9 (igual que vigas y muros).

**Instanciación sugerida** (desde el formulario principal del módulo 08):
```vb
Dim rep As New Form_Reporte_VigasFundacion() With {
    .VigasFundacion = _proyecto.Elementos.VigasFundacion
}
rep.ShowDialog(Me)
```

**Why:** Módulo 08 no tenía reporte de revisión; se creó siguiendo el patrón de Form_Reporte_Resumen_Nervios.

**How to apply:** Para futuros cambios al reporte de vigas de fundación, leer los campos de cVigaFundacion antes de asumir que existen nuevos campos (usar `<OptionalField>` si se agregan).
