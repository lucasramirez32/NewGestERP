using NewGest.Domain.Entities.Com;

namespace NewGest.Application.Services;

public interface IQrFiscalService
{
    string GenerarUrl(Comprobante comprobante, string cuitEmisor);
    byte[] GenerarPng(string url);
}
