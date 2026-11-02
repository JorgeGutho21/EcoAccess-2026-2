#pragma once
static_assert(sizeof(unsigned) == 4, "El reloj necesita enteros sin signo de 32 bits");

// Estado independiente de Arduino para probar dos órdenes consecutivas y el wrap del reloj.
class ConexionServos {
  bool uno = false, dos = false;
public:
  bool Preparar(bool (*adjuntar)(int)) {
    if (!uno) uno = adjuntar(18);
    if (!dos) dos = adjuntar(19);
    return uno && dos;
  }
};
class VentanaDesafio {
  bool activo = false;
  unsigned inicio = 0;
public:
  void Iniciar(unsigned ahora) { inicio = ahora; activo = true; }
  unsigned Edad(unsigned ahora) const { return ahora - inicio; }
  void Actualizar(unsigned ahora) { if (activo && Edad(ahora) >= 5000) activo = false; }
  bool Vigente(unsigned ahora, unsigned plazo) const { return activo && plazo <= 5000 && Edad(ahora) < plazo; }
  void Consumir() { activo = false; }
};
