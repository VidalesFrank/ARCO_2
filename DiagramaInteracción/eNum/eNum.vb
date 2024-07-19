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
        Marselo
        Frank
        Anlly
        Natali
        Daniela
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

End Class
