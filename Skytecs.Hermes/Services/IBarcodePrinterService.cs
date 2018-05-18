using Skytecs.Hermes.Models;

namespace Skytecs.Hermes.Services
{
    public interface IBarcodePrinterService
    {
        bool Send(BarcodePrinterData data);
    }
}