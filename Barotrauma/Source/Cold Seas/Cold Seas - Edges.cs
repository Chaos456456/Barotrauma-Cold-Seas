using Barotrauma;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voronoi2;

namespace Voronoi2
{
    partial class GraphEdge
    {
        public bool isInternal = false;
        public PrimaryNode node1;
        public PrimaryNode node2;

        public List<SecondaryNode> secondaryNodes = new List<SecondaryNode>();

        public SecondaryNode AddSecondaryNode()
        {
            List<int> validDistances = new List<int>();
            if (Vector2.Distance(node1.mapPosition, node2.mapPosition) <= 50.0f)
                return (null);


            for (int i = 1; i < 10; i++)
            {
                bool tooClose = false;
                Vector2 lerp = Vector2.Lerp(node1.mapPosition, node2.mapPosition, i);

                foreach (SecondaryNode secNode in secondaryNodes)
                {
                    if (Vector2.Distance(lerp, secNode.mapPosition) <= 25)
                        tooClose = true;
                }

                if (tooClose == false)
                    validDistances.Add(i);
            }

            if (validDistances.Count == 0)
                return (null);

            int distanceIndex = Rand.Range(0, validDistances.Count - 1, false);

            SecondaryNode newNode = new SecondaryNode(Vector2.Lerp(node1.mapPosition, node2.mapPosition, (float)validDistances[distanceIndex] / 10), this);

            secondaryNodes.Add(newNode);

            return (newNode);
        }
    }
}
