Imports System.Security.Cryptography.X509Certificates
Imports ARCO.eNumeradores

<Serializable>
Module DirectorioResponsables

    Public Property dResponsables As New Dictionary(Of eResponsables, Persona)()

    Sub New()
        dResponsables.Add(eResponsables.Mayra, New Persona("Mayra Alejandra Zea Acevedo"))
        dResponsables.Add(eResponsables.Marselo, New Persona("Marselo Marulanda López"))
        dResponsables.Add(eResponsables.Frank, New Persona("Frank Daniel Vidales Herrera"))
        dResponsables.Add(eResponsables.Anlly, New Persona("Anlly Valentina Granda Chaverra"))
        dResponsables.Add(eResponsables.Natali, New Persona("Natali Ramírez Pérez"))
        dResponsables.Add(eResponsables.Daniela, New Persona("Daniela Lopera Ramírez"))
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

