using System.Threading.Tasks;
using ControleConsolidado.Models;

namespace ControleConsolidado.Repositories
{
    public interface IConsolidadoRepository
    {
        Task SaveAsync(Consolidado consolidado);
        Task<Consolidado> GetByIdAsync(int id);
    }
}
