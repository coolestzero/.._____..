using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Services
{
    public interface IUseMdbService
    {
        Task<string> GetCarNameAsync(string pzdm);
        //Task<List<UserModel>> GetAllUsersAsync();
        //Task<UserModel> GetUserByNameAsync(string userName);
        //Task<bool> AddUserAsync(UserModel user);
        //Task<bool> UpdateUserAsync(UserModel user);
    }
}
