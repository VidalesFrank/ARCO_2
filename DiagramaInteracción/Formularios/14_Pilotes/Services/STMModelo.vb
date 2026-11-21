''' <summary>
''' Modelo puntal-tensor (NSR-10 C.23) de un dado de pilotes.
''' Clases en memoria — se regeneran en cada evaluación, no se serializan.
''' Convención de ejes locales del dado:
'''   X horizontal (lado LadoColumna_X), Y horizontal (lado LadoColumna_Y),
'''   Z vertical hacia arriba. Origen en el centroide del polígono a nivel
'''   del acero inferior (d medido desde la cara superior del dado).
''' </summary>
Public Class ModeloSTM
    Public Property NodoSuperior As NodoSTM
    Public Property NodosInferiores As New List(Of NodoSTM)
    Public Property Puntales As New List(Of PuntalSTM)
    Public Property TensoresX As New List(Of TensorSTM)
    Public Property TensoresY As New List(Of TensorSTM)

    ''' <summary>Altura efectiva d usada [m].</summary>
    Public Property D_Efectivo As Double

    ''' <summary>Relación de esbeltez L_claro / h del dado (orientativa).</summary>
    Public Property RelacionLH As Double
End Class

Public Enum eTipoNodoSTM
    CCC = 0  ' tres o más compresiones, ningún tensor
    CCT = 1  ' dos compresiones + un tensor
    CTT = 2  ' una compresión + dos o más tensores
End Enum

''' <summary>
''' Nodo del modelo STM. Representa un punto de intersección de puntales y
''' tensores. Los nodos inferiores encima de los pilotes son CCT (un puntal
''' bajando + dos tensores ortogonales). El superior bajo la columna es CCC.
''' </summary>
Public Class NodoSTM
    Public Property Nombre As String = ""
    Public Property X As Double
    Public Property Y As Double
    Public Property Z As Double
    Public Property Tipo As eTipoNodoSTM = eTipoNodoSTM.CCC

    ''' <summary>
    ''' Área nodal usada para el chequeo del nodo [m²]. Se calcula como el
    ''' área de apoyo (columna para nodo superior, pilote para nodo inferior)
    ''' proyectada sobre el plano perpendicular al puntal más cargado.
    ''' </summary>
    Public Property AreaNodal As Double

    ''' <summary>Fuerza de compresión que llega al nodo [kN].</summary>
    Public Property FuerzaCompresion As Double

    ''' <summary>βn según tipo (1.0 / 0.80 / 0.60 — NSR-10 C.23.9.2).</summary>
    Public Property BetaN As Double

    ''' <summary>fcu nodal = 0.85·βn·fc [MPa].</summary>
    Public Property FCU As Double

    ''' <summary>Resistencia nominal φFn = φ·fcu·Anz [kN]. φ=0.75.</summary>
    Public Property PhiFn As Double

    ''' <summary>C/D del nodo = φFn / FuerzaCompresion. ≥ 1 cumple.</summary>
    Public Property CD As Double
End Class

''' <summary>
''' Puntal del modelo STM (línea de compresión inclinada del nodo superior
''' a un nodo inferior). Las propiedades se llenan en la evaluación.
''' </summary>
Public Class PuntalSTM
    Public Property NodoSup As NodoSTM
    Public Property NodoInf As NodoSTM

    ''' <summary>Fuerza axial de compresión del puntal [kN] = Pu / sin(θ).</summary>
    Public Property Fuerza As Double

    ''' <summary>Ángulo del puntal con la horizontal [grados].</summary>
    Public Property Angulo As Double

    ''' <summary>Longitud del puntal [m].</summary>
    Public Property Longitud As Double

    ''' <summary>Ancho (lado menor) del puntal usado para Acs [m].</summary>
    Public Property AnchoPuntal As Double

    ''' <summary>Espesor del puntal usado para Acs [m] = espesor del dado.</summary>
    Public Property EspesorPuntal As Double

    ''' <summary>Área transversal del puntal [m²] = ancho × espesor.</summary>
    Public Property Acs As Double

    ''' <summary>βs del puntal (NSR-10 C.23.3.3).</summary>
    Public Property BetaS As Double

    ''' <summary>fcu del puntal = 0.85·βs·fc [MPa].</summary>
    Public Property FCU As Double

    ''' <summary>Resistencia nominal φFns = φ·fcu·Acs [kN]. φ=0.75.</summary>
    Public Property PhiFns As Double

    ''' <summary>C/D del puntal. ≥ 1 cumple.</summary>
    Public Property CD As Double
End Class

''' <summary>
''' Tensor del modelo STM (línea horizontal de tracción en la base del dado,
''' resistida por acero de refuerzo). Hay un tensor por cada par de pilotes
''' que comparten una fila (dirección X) o una columna (dirección Y).
''' </summary>
Public Class TensorSTM
    Public Property NodoInfA As NodoSTM
    Public Property NodoInfB As NodoSTM

    ''' <summary>Dirección del tensor: "X" o "Y" (ejes locales del dado).</summary>
    Public Property Direccion As String = ""

    ''' <summary>Fuerza de tracción del tensor [kN] = Pu · cot(θ) acumulado.</summary>
    Public Property Fuerza As Double

    ''' <summary>Longitud del tensor [m] (distancia entre nodos inferiores).</summary>
    Public Property Longitud As Double

    ''' <summary>Área de acero requerida [cm²] = Fuerza / (φ·fy), φ=0.75.</summary>
    Public Property As_Requerida_cm2 As Double
End Class
