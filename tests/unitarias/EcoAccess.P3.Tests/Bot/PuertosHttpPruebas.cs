using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace EcoAccess.P3.Tests.Bot
{

    /// <summary>
    /// Evita reutilizar prefijos HTTP del mismo proceso cuando MSTest ejecuta métodos en paralelo.
    /// Son candidatos, no una reserva del sistema: el servidor real sigue comprobando el enlace.
    /// </summary>
    internal static class PuertosHttpPruebas
    {
        private static readonly HashSet<int> Asignados = new HashSet<int>();

        public static int Siguiente()
        {
            lock (Asignados)
            {
                for (int intento = 0; intento < 100; intento++)
                {
                    var socket = new TcpListener(IPAddress.Loopback, 0);
                    socket.Server.ExclusiveAddressUse = true;
                    socket.Start();
                    int puerto = ((IPEndPoint)socket.LocalEndpoint).Port;
                    socket.Stop();
                    if (Asignados.Add(puerto)) return puerto;
                }
            }
            throw new InvalidOperationException("No se encontró un puerto de prueba HTTP sin reutilizar.");
        }
    }
}
