using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voronoi2;

namespace Voronoi2
{
    partial class VoronoiCell
    {
        public enum CELLSTATE
        {
            BLANK = -1,
            UNASSIGNED = 0,
            ASSIGNED = 1
        }

        public int group = -1;
        public CELLSTATE cellState = CELLSTATE.UNASSIGNED;
        public bool hasGap = false;

        public VoronoiCell getAdjacentCellFromEdge(GraphEdge edge)
        {
            return (edge.cell1 == this ? edge.cell2 : edge.cell1);
        }

        public List<GraphEdge> getValidEdges()
        {
            List<GraphEdge> validEdges = new List<GraphEdge>();

            foreach(GraphEdge edge in edges)
            {
                if (getAdjacentCellFromEdge(edge) != null)
                    validEdges.Add(edge);
            }
            return (validEdges);
        }

        public List<GraphEdge> getValidEdgesGroup()
        {
            List<GraphEdge> validEdges = new List<GraphEdge>();

            foreach (GraphEdge edge in edges)
            {
                if (getAdjacentCellFromEdge(edge) != null && getAdjacentCellFromEdge(edge).cellState == CELLSTATE.UNASSIGNED)
                    validEdges.Add(edge);
            }
            return (validEdges);
        }
    }
}
