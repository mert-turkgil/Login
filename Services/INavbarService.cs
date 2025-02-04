using System.Threading.Tasks;
using Alpha.Models;

namespace Alpha.Services
{
    public interface INavbarService
    {
        Task<NavbarViewModel> GetNavbarViewModelAsync();
    }
}
