using DropcoreApi.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DropcoreApi.Core.Auth;

public interface IPasswordHasher
{
    PasswordHash Hash(Password password);
}