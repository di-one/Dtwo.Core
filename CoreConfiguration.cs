using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Core.Configuration
{
    [DataContract]
    public class CoreConfiguration
    {
        [DataMember]
        public bool LoadI18 { get; set; } = false;
    }
}
