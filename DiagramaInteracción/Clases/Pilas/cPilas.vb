<Serializable>
Public Class cPilas

    Public ListaElementos As New List(Of Elemento_Pila)

    Public Esf_Adm_Est As Single
    Public Esf_Adm_Din As Single
    Public Esf_Frccion As Single
    Public ModuloE_Acero As Single
    Public Def_Uni_ConcAs As Single
    Public Fy As Single
    Public Opcion_Elemento As String

    Public Tabla_JointReactions As DataTable

    Public Reactions As New List(Of cCombinacionPila)

    Public Lista_Combinaciones As New List(Of String)

    Public Lista_Combinaciones_Gravitacionales_Servicio As New List(Of String)
    Public Lista_Combinaciones_Gravitacionales_Design As New List(Of String)

    Public Lista_Combinaciones_Sismicas_Servicio As New List(Of String)
    Public Lista_Combinaciones_Sismicas_Design As New List(Of String)

    Public Lista_Combinaciones_Traccion As New List(Of String)


End Class
