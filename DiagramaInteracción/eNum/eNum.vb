<Serializable>
Public Class eNumeradores
    Public Enum eSistemaEstructural
        Porticos
        Combinado
        MCR
    End Enum

    Public Class Sistema
        Public Property NameSistema As String

        Public Sub New(Sistema As String)
            Me.NameSistema = Sistema
        End Sub
    End Class

    Public Enum eDireccion
        X
        Y
    End Enum

    Public Enum eTipoMuro
        Protagonico
        Complemento
    End Enum

    Public Enum eDisipasion
        DES
        DMO
        DMI
    End Enum

    Public Enum eAMZ
        ALTA
        INTERMEDIA
        BAJA
    End Enum

    Public Enum eGrupoSuelo
        A
        B
        C
        D
        E
        F
    End Enum

    Public Enum eGrupoUso
        I
        II
        III
        IV
    End Enum

    Public Enum eResponsables
        Mayra
        Alejandra
        Frank
        Anlly
        Natali
        Otros
    End Enum

    Public Class Persona
        Public Property NombreCompleto As String

        Public Sub New(nombreCompleto As String)
            Me.NombreCompleto = nombreCompleto
        End Sub
    End Class

    Public Enum ColumnaFuerzas
        Piso = 0
        Label = 1
        Combinacion = 2
        Location = 3
        P = 4
        V2 = 5
        V3 = 6
        T = 7
        M2 = 8
        M3 = 9
    End Enum

    Public Enum MallaTipo
        None
        D_84
        D_106
        D_131
        D_158
        D_188
        D_221
        D_257
        D_295
        D_335
    End Enum

    Public Enum BarraTipo
        None
        Num_2
        Num_3
        Num_4
        Num_5
        Num_6
        Num_7
        Num_8
        Num_10
        Users
    End Enum

    Public Enum T_Seccion
        Rectangular
        T
        L
        Circular
    End Enum


    Public Enum TipoCombinacion
        ServicioEstatica
        ServicioDinamica
        UltimaEstatica
        UltimaDinamica
    End Enum

    Public Property Tipo As TipoCombinacion


    Public Enum eDireccionRefuerzo
        L1
        L2
    End Enum

    Public Enum eTipoRefuerzo
        Superior
        Inferior
    End Enum

    Public Enum PosicionTramoViga
        Izquierda = 0
        Centro = 1
        Derecha = 2
    End Enum


End Class
