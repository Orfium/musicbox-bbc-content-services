using MusicManager.Logics.Logics;
using System;
using System.Collections.Generic;
using System.Text;

namespace LabelProcess.Logics
{
    public class PRSearchLogic
    {
        private readonly ICtagLogic _ctagLogic;

        public PRSearchLogic(ICtagLogic ctagLogic)
        {
            _ctagLogic = ctagLogic;
        }


    }
}
