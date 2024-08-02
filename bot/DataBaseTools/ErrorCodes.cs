using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bot
{
    public enum ExecutionStatus 
    { 
        Success = 0,
        NullReferenseError = 1,
        UserAlreadyExistsError = 2,
        EventAlreadyExistsError = 3,
        EventDoesNotExistsError = 4,
        DatabaseError = 5,
        InsufficientRightsError = 6
    }
}
