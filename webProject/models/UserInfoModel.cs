using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using webProject.Entities;

namespace webProject.Models
{
    public class UserInfoModel
    {
        public UserInfoModel(User user) {
            Login = user.Login;
            Role = user.Role;
            Username = user.Username;
            Id = user.Id;
        }
        public long Id { get; set; }
        public string Login { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
    }
}