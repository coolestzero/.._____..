using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services.Mdb
{
    public interface ICarDataService
    {
        Task<string> GetCarNameAsync(string pzdm);
    }
}
