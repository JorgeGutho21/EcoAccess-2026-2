namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Fila del catálogo de roles. El usuario no guarda objetos Rol sino su <see cref="CodigoRol"/>;
    /// esta clase sirve para listar los roles con su descripción y para comprobar que el catálogo
    /// de la base de datos coincide con la enumeración.
    /// </summary>
    public class Rol
    {
        public const int LongitudMaximaDescripcion = 200;

        public int Id { get; }
        public CodigoRol Codigo { get; }
        public string Descripcion { get; }

        public Rol(int id, CodigoRol codigo, string descripcion)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            Codigo = Validacion.EnumDefinido(codigo, nameof(codigo));
            Descripcion = Validacion.TextoRequerido(descripcion, nameof(descripcion), LongitudMaximaDescripcion);
        }
    }
}
