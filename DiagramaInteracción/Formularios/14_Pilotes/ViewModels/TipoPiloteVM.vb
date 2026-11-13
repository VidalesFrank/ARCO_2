''' <summary>
''' Wrapper observable sobre TipoPilote para edición en el catálogo.
''' Modifica el modelo in-place, de modo que el .esm existente lo persiste.
''' </summary>
Public Class TipoPiloteVM
    Inherits ObservableVM

    Public ReadOnly Modelo As TipoPilote

    Public Sub New(modelo As TipoPilote)
        Me.Modelo = modelo
    End Sub

    Public Property Nombre As String
        Get
            Return Modelo.Nombre
        End Get
        Set(value As String)
            If Modelo.Nombre <> value Then
                Modelo.Nombre = If(value, "")
                OnPropertyChanged()
                OnPropertyChanged(NameOf(Descripcion))
            End If
        End Set
    End Property

    Public Property Tipo As eTipoPilote
        Get
            Return Modelo.Tipo
        End Get
        Set(value As eTipoPilote)
            If Modelo.Tipo <> value Then
                Modelo.Tipo = value
                OnPropertyChanged()
                OnPropertyChanged(NameOf(Descripcion))
            End If
        End Set
    End Property

    Public Property Diametro As Double
        Get
            Return Modelo.Diametro
        End Get
        Set(value As Double)
            If Modelo.Diametro <> value Then
                Modelo.Diametro = value
                OnPropertyChanged()
                OnPropertyChanged(NameOf(Descripcion))
            End If
        End Set
    End Property

    Public Property Longitud As Double
        Get
            Return Modelo.Longitud
        End Get
        Set(value As Double)
            If Modelo.Longitud <> value Then
                Modelo.Longitud = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Qc_Admisible As Double
        Get
            Return Modelo.Qc_Admisible
        End Get
        Set(value As Double)
            If Modelo.Qc_Admisible <> value Then
                Modelo.Qc_Admisible = value
                OnPropertyChanged()
                OnPropertyChanged(NameOf(Descripcion))
            End If
        End Set
    End Property

    Public Property Qt_Admisible As Double
        Get
            Return Modelo.Qt_Admisible
        End Get
        Set(value As Double)
            If Modelo.Qt_Admisible <> value Then
                Modelo.Qt_Admisible = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    ''' <summary>Resumen corto para listas (ej. en el combo de un pilote).</summary>
    Public ReadOnly Property Descripcion As String
        Get
            Return $"{Nombre} · Ø{Diametro * 1000:0}mm · Qc={Qc_Admisible:0} kN"
        End Get
    End Property

    Public ReadOnly Property EsMicropilote As Boolean
        Get
            Return Tipo = eTipoPilote.Micropilote
        End Get
    End Property

End Class
