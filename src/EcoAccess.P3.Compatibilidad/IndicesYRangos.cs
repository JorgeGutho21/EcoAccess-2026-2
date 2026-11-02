namespace System
{
    /// <summary>Índice desde el inicio o el final, requerido por la sintaxis ^n y a..b.</summary>
    public readonly struct Index
    {
        private readonly int _valor;
        public Index(int valor, bool desdeFinal = false) { _valor = desdeFinal ? ~valor : valor; }
        public static Index Start => new Index(0);
        public static Index End => new Index(0, true);
        public int Value => _valor < 0 ? ~_valor : _valor;
        public bool IsFromEnd => _valor < 0;
        public int GetOffset(int longitud) => _valor < 0 ? longitud + _valor + 1 : _valor;
        public static implicit operator Index(int valor) => new Index(valor);
    }

    /// <summary>Rango entre dos índices, requerido por la sintaxis a..b.</summary>
    public readonly struct Range
    {
        public Range(Index inicio, Index fin) { Start = inicio; End = fin; }
        public Index Start { get; }
        public Index End { get; }
        public static Range StartAt(Index inicio) => new Range(inicio, Index.End);
        public static Range EndAt(Index fin) => new Range(Index.Start, fin);
        public static Range All => new Range(Index.Start, Index.End);
        public (int Offset, int Length) GetOffsetAndLength(int longitud)
        {
            var i = Start.GetOffset(longitud);
            return (i, End.GetOffset(longitud) - i);
        }
    }
}
