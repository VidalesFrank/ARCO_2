''' <summary>
''' Cálculo de losas nervadas en dos direcciones por el método de coeficientes.
'''
''' Se extrajo de Form_03_Losas.Button2_Click, que tenía 175 líneas mezclando
''' lectura de cuadros de texto, cálculo estructural y llenado de tablas. Aquí
''' queda solo el cálculo: funciones puras, sin UI, y por tanto verificables.
'''
''' La cadena de casos se trasladó mecánicamente desde el formulario — no se
''' retipeó — para no introducir errores de transcripción en una tabla de nueve
''' casos con más de veinte combinaciones.
''' </summary>
Public NotInheritable Class LosaService

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Estado de un borde de la losa. SinDefinir reproduce el caso real en que
    ''' el usuario no marcó ninguna de las dos casillas del borde: entonces
    ''' ninguna combinación aplica y el caso queda en 0, igual que antes.
    ''' </summary>
    Public Enum Borde
        SinDefinir = 0
        Continuo = 1
        Discontinuo = 2
    End Enum

    ''' <summary>
    ''' Caso 1 a 9 del método de coeficientes, según la continuidad de los cuatro
    ''' bordes y cuál de los dos lados es el corto.
    ''' </summary>
    ''' <param name="ladoCortoEsX">
    ''' True cuando el lado corto (Lna) es el de la dirección X — en el
    ''' formulario, cuando Lna coincide con L1. Cambia a qué caso corresponde una
    ''' misma combinación de bordes, porque el caso se define respecto al lado
    ''' corto, no respecto a la pantalla.
    ''' </param>
    ''' <returns>El número de caso, o 0 si la combinación no corresponde a ninguno.</returns>
    Public Shared Function DeterminarCaso(izq As Borde, sup As Borde, der As Borde, inf As Borde,
                                          ladoCortoEsX As Boolean) As Integer

        If izq = Borde.Discontinuo And sup = Borde.Discontinuo And der = Borde.Discontinuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 1
        ElseIf izq = Borde.Continuo And sup = Borde.Continuo And der = Borde.Continuo And inf = Borde.Continuo Then
            DeterminarCaso = 2
        ElseIf Not ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 3
        ElseIf ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 3
        ElseIf izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 4
        ElseIf izq = Borde.Continuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 4
        ElseIf izq = Borde.Discontinuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Continuo Then
            DeterminarCaso = 4
        ElseIf izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 4
        ElseIf Not ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 5
        ElseIf ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 5
        ElseIf Not ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 6
        ElseIf Not ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Discontinuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 6
        ElseIf ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Discontinuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 6
        ElseIf ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 6
        ElseIf Not ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Discontinuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 7
        ElseIf Not ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 7
        ElseIf ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 7
        ElseIf ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Discontinuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 7
        ElseIf Not ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Continuo Then
            DeterminarCaso = 8
        ElseIf Not ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Continuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 8
        ElseIf ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Continuo And inf = Borde.Continuo Then
            DeterminarCaso = 8
        ElseIf ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 8
        ElseIf Not ladoCortoEsX And izq = Borde.Discontinuo And sup = Borde.Continuo And der = Borde.Continuo And inf = Borde.Continuo Then
            DeterminarCaso = 9
        ElseIf Not ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Continuo And der = Borde.Discontinuo And inf = Borde.Continuo Then
            DeterminarCaso = 9
        ElseIf ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Discontinuo And der = Borde.Continuo And inf = Borde.Continuo Then
            DeterminarCaso = 9
        ElseIf ladoCortoEsX And izq = Borde.Continuo And sup = Borde.Continuo And der = Borde.Continuo And inf = Borde.Discontinuo Then
            DeterminarCaso = 9
        End If

    End Function

    ''' <summary>
    ''' Peso propio de la losa nervada, en kN/m². Se obtiene del volumen de
    ''' concreto de un módulo (loseta superior más los dos nervios que le
    ''' corresponden) dividido por el área en planta de ese módulo, por 24 kN/m³.
    ''' </summary>
    Public Shared Function PesoPropioLosa(espesorLoseta As Double, alturaNervio As Double,
                                          anchoNervio As Double,
                                          separacionX As Double, separacionY As Double) As Double

        Dim moduloX As Double = separacionX + anchoNervio
        Dim moduloY As Double = separacionY + anchoNervio
        If moduloX <= 0 OrElse moduloY <= 0 Then Return 0

        Dim volLoseta As Double = espesorLoseta * moduloX * moduloY
        Dim volNervio As Double = anchoNervio * (separacionX + separacionY + anchoNervio) *
                                  (alturaNervio - espesorLoseta)

        Return 24.0 * (volLoseta + volNervio) / (moduloX * moduloY)

    End Function

    ''' <summary>Combinación mayorada 1.2 CM + 1.6 CV (NSR-10 B.2.4).</summary>
    Public Shared Function CargaUltima(cargaMuerta As Double, cargaViva As Double) As Double
        Return 1.2 * cargaMuerta + 1.6 * cargaViva
    End Function

    ''' <summary>Momentos y cortantes de una franja de la losa.</summary>
    Public Structure Demandas
        Public Ma_Neg As Double
        Public Mb_Neg As Double
        Public Ma_Pos As Double
        Public Mb_Pos As Double
        Public Va As Double
        Public Vb As Double
    End Structure

    ''' <summary>
    ''' Demandas de la franja central a partir de los ocho coeficientes del caso.
    ''' Orden del arreglo, igual que lo devuelve Coeficientes(): Ca-, Cb-, CaD,
    ''' CbD, CaL, CbL, qa, qb.
    '''
    ''' Los momentos negativos usan la carga última completa; los positivos
    ''' separan muerta y viva porque llevan coeficientes distintos.
    ''' </summary>
    Public Shared Function CalcularDemandasFranjaCentral(coef() As Single,
                                                         cargaMuerta As Double, cargaViva As Double,
                                                         lna As Double, lnb As Double,
                                                         sa As Double, sb As Double,
                                                         anchoNervio As Double) As Demandas

        If coef Is Nothing OrElse coef.Length < 8 Then Return New Demandas()

        Dim cu As Double = CargaUltima(cargaMuerta, cargaViva)
        Dim anchoA As Double = sa + anchoNervio
        Dim anchoB As Double = sb + anchoNervio

        Return New Demandas With {
            .Ma_Neg = coef(0) * cu * lna ^ 2 * anchoA,
            .Mb_Neg = coef(1) * cu * lnb ^ 2 * anchoB,
            .Ma_Pos = (coef(2) * 1.2 * cargaMuerta + coef(4) * 1.6 * cargaViva) * lna ^ 2 * anchoA,
            .Mb_Pos = (coef(3) * 1.2 * cargaMuerta + coef(5) * 1.6 * cargaViva) * lnb ^ 2 * anchoB,
            .Va = coef(6) * cu * lna / 2 * anchoA,
            .Vb = coef(7) * cu * lnb / 2 * anchoB
        }

    End Function

    ''' <summary>
    ''' La franja de borde toma un tercio de la franja central, como establece el
    ''' método de coeficientes.
    ''' </summary>
    Public Shared Function ATercios(central As Demandas) As Demandas
        Return New Demandas With {
            .Ma_Neg = central.Ma_Neg / 3.0,
            .Mb_Neg = central.Mb_Neg / 3.0,
            .Ma_Pos = central.Ma_Pos / 3.0,
            .Mb_Pos = central.Mb_Pos / 3.0,
            .Va = central.Va / 3.0,
            .Vb = central.Vb / 3.0
        }
    End Function

End Class
