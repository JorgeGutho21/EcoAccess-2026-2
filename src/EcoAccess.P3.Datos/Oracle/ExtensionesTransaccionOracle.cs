using System.Threading;
using System.Threading.Tasks;

namespace Oracle.ManagedDataAccess.Client
{
    /// <summary>Transacciones con la firma asíncrona que usan los repositorios; el proveedor para .NET Framework las ejecuta de forma síncrona.</summary>
    internal static class ExtensionesTransaccionOracle
    {
        internal static Task<OracleTransaction> BeginTransactionAsync(this OracleConnection conexion, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(conexion.BeginTransaction());
        }

        internal static Task<OracleTransaction> BeginTransactionAsync(this OracleConnection conexion, System.Data.IsolationLevel nivel, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(conexion.BeginTransaction(nivel));
        }

        internal static Task CommitAsync(this OracleTransaction transaccion, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            transaccion.Commit();
            return Task.CompletedTask;
        }

        internal static Task RollbackAsync(this OracleTransaction transaccion, CancellationToken ct = default)
        {
            transaccion.Rollback();
            return Task.CompletedTask;
        }
    }
}
