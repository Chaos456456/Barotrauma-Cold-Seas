using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barotrauma
{
    partial class LocationConnection
    {
        public PrimaryNode[] primaryNodes;
        public SecondaryNode[] secondaryNodes;

        public LocationConnection(PrimaryNode node1, PrimaryNode node2)
        {
            primaryNodes = new PrimaryNode[] { node1, node2 };

            missionsCompleted = 0;
        }

        public LocationConnection(SecondaryNode node1, SecondaryNode node2)
        {
            secondaryNodes = new SecondaryNode[] { node1, node2 };

            missionsCompleted = 0;
        }
    }
}
