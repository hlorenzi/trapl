using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Grammar
{
    public class TypeInferencer
    {
        public int typeNum;


        public int AddSlot()
        {
            this.typeNum++;
            return this.typeNum - 1;
        }
    }
}
