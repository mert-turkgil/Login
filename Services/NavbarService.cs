using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Models;

namespace Alpha.Services
{
    public class NavbarService : INavbarService
    {

        public NavbarService()
        {
        }

        public async Task<NavbarViewModel> GetNavbarViewModelAsync()
        {
            return new NavbarViewModel
            {
            };
        }
    }
}