namespace GUI
{
    // Extraído de CobroSuscripcionForm/RenovacionSuscripcionForm — misma clase estaba
    // duplicada idéntica en ambos (wrapper de BE.Cliente para el ComboBox de selección).
    internal sealed class ClienteItem
    {
        public BE.Cliente Cliente { get; }
        public ClienteItem(BE.Cliente c) => Cliente = c;
        public override string ToString() => $"{Cliente.NombreCompleto} (DNI {Cliente.DNI})";
    }
}
