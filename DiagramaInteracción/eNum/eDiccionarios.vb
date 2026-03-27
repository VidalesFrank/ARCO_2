Imports ARCO.eNumeradores

<Serializable>
Module DirectorioResponsables

    Public Property dResponsables As New Dictionary(Of eResponsables, Persona)()

    Sub New()
        dResponsables.Add(eResponsables.Mayra, New Persona("Mayra Alejandra Zea Acevedo"))
        dResponsables.Add(eResponsables.Frank, New Persona("Frank Daniel Vidales Herrera"))
        dResponsables.Add(eResponsables.Anlly, New Persona("Anlly Valentina Granda Chaverra"))
        dResponsables.Add(eResponsables.Natali, New Persona("Natali Ramírez Pérez"))
        dResponsables.Add(eResponsables.Alejandra, New Persona("Maria Alejandra Aladino Madrigal"))
        dResponsables.Add(eResponsables.Otros, New Persona("Otros"))
    End Sub

End Module

Module DirectorioSistemaEstructural
    Public Property dSistemaEstructural As New Dictionary(Of eSistemaEstructural, Sistema)()

    Sub New()
        dSistemaEstructural.Add(eSistemaEstructural.MCR, New Sistema("Muros de CR"))
        dSistemaEstructural.Add(eSistemaEstructural.Combinado, New Sistema("Sistema Combinado de Muros y Porticos de CR"))
        dSistemaEstructural.Add(eSistemaEstructural.Porticos, New Sistema("Porticos de CR"))
    End Sub

End Module

Module DirectorioDepartamentos

    Public Property departamentos As New Dictionary(Of String, String)

    Sub New()
        departamentos.Add("AMZ", "Amazonas")
        departamentos.Add("ANT", "Antioquia")
        departamentos.Add("ARA", "Arauca")
        departamentos.Add("ATL", "Atlántico")
        departamentos.Add("BOL", "Bolívar")
        departamentos.Add("BOY", "Boyacá")
        departamentos.Add("CAL", "Caldas")
        departamentos.Add("CAQ", "Caquetá")
        departamentos.Add("CAS", "Casanare")
        departamentos.Add("CAU", "Cauca")
        departamentos.Add("CES", "Cesar")
        departamentos.Add("CHO", "Chocó")
        departamentos.Add("CUN", "Cundinamarca")
        departamentos.Add("COR", "Córdoba")
        departamentos.Add("GUA", "Guainía")
        departamentos.Add("GUV", "Guaviare")
        departamentos.Add("HUI", "Huila")
        departamentos.Add("LAG", "La Guajira")
        departamentos.Add("MAG", "Magdalena")
        departamentos.Add("MET", "Meta")
        departamentos.Add("NAR", "Nariño")
        departamentos.Add("NDS", "Norte de Santander")
        departamentos.Add("PUT", "Putumayo")
        departamentos.Add("QUI", "Quindío")
        departamentos.Add("RIS", "Risaralda")
        departamentos.Add("SAP", "San Andrés y Providencia")
        departamentos.Add("SAN", "Santander")
        departamentos.Add("SUC", "Sucre")
        departamentos.Add("TOL", "Tolima")
        departamentos.Add("VAC", "Valle del Cauca")
        departamentos.Add("VAU", "Vaupés")
        departamentos.Add("VIC", "Vichada")
    End Sub
End Module

Module DirectorioCiudades

    Public Property Ciudades As New Dictionary(Of String, String)

    Sub New()
        Ciudades.Add("MED", "Medellín")
        Ciudades.Add("RIO", "Rionegro")
        Ciudades.Add("ENV", "Envigado")
        Ciudades.Add("SAB", "Sabaneta")
        Ciudades.Add("BEL", "Bello")
        Ciudades.Add("ITA", "Itagüi")
        Ciudades.Add("MAR", "Marinilla")
        Ciudades.Add("LAC", "La Ceja")
        Ciudades.Add("RET", "El Retiro")
        Ciudades.Add("LAU", "La Unión")

    End Sub

End Module

Module MallaData
    Public ReadOnly MallaAreas As New Dictionary(Of MallaTipo, Single) From {
        {MallaTipo.None, 0},
        {MallaTipo.D_84, 84},
        {MallaTipo.D_106, 106},
        {MallaTipo.D_131, 131},
        {MallaTipo.D_158, 158},
        {MallaTipo.D_188, 188},
        {MallaTipo.D_221, 221},
        {MallaTipo.D_257, 257},
        {MallaTipo.D_295, 295},
        {MallaTipo.D_335, 335}
    }
End Module

Module BarraData
    Public ReadOnly BarraAreas As New Dictionary(Of String, Single) From {
        {"#2", 32},
        {"#3", 71},
        {"#4", 129},
        {"#5", 199},
        {"#6", 284},
        {"#7", 387},
        {"#8", 510},
        {"#10", 819}
    }

    Public ReadOnly BarraDb As New Dictionary(Of String, Single) From {
        {"#2", 6.4},
        {"#3", 9.5},
        {"#4", 12.7},
        {"#5", 15.9},
        {"#6", 19.1},
        {"#7", 22.2},
        {"#8", 25.4},
        {"#10", 32.3}
    }

    Public ReadOnly BarraTipoMap As New Dictionary(Of BarraTipo, String) From {
        {BarraTipo.None, "None"},
        {BarraTipo.Num_2, "#2"},
        {BarraTipo.Num_3, "#3"},
        {BarraTipo.Num_4, "#4"},
        {BarraTipo.Num_5, "#5"},
        {BarraTipo.Num_6, "#6"},
        {BarraTipo.Num_7, "#7"},
        {BarraTipo.Num_8, "#8"},
        {BarraTipo.Num_10, "#10"},
        {BarraTipo.Users, "Custom"}
    }
End Module




