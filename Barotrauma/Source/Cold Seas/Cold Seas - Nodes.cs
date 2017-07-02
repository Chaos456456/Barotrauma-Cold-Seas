using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voronoi2;


namespace Barotrauma
{
    class PrimaryNode
    {
        public Vector2 mapPosition;
        public List<VoronoiCell> adjacentCells;
        public List<GraphEdge> connectedEdges;
        public List<LocationConnection> connections;

        public int group = -1;

        public bool aggregate = false;

        public bool hasCell = false;

        public bool invalid = false;

        public PrimaryNode(Vector2 newPosition)
        {
            mapPosition = newPosition;
            adjacentCells = new List<VoronoiCell>();
            connectedEdges = new List<GraphEdge>();
            connections = new List<LocationConnection>();
        }

        public void AddEdge(GraphEdge edge, PrimaryNode newNode = null)
        {
            connectedEdges.Add(edge);
            if (!adjacentCells.Contains(edge.cell1))
                adjacentCells.Add(edge.cell1);
            if (!adjacentCells.Contains(edge.cell2))
                adjacentCells.Add(edge.cell2);
            if (edge.node1 == newNode)
                edge.node1 = this;
            else if (edge.node2 == newNode)
                edge.node2 = this;

            group = edge.cell1.cellState == VoronoiCell.CELLSTATE.ASSIGNED ? edge.cell1.group : edge.cell2.group;
        }
    }

    class SecondaryNode
    {
        public bool isOnEdge;
        public Vector2 mapPosition;
        public VoronoiCell cell;
        public List<LocationConnection> connections;
        public GraphEdge attachedEdge;
        public List<SecondaryNode> attachedNodes;

        public SecondaryNode(Vector2 newPosition, VoronoiCell sourceCell)
        {
            mapPosition = newPosition;
            cell = sourceCell;
            connections = new List<LocationConnection>();
            attachedNodes = new List<SecondaryNode>();
            isOnEdge = false;
        }

        public SecondaryNode(Vector2 newPosition, GraphEdge sourceEdge)
        {
            mapPosition = newPosition;
            attachedEdge = sourceEdge;
            connections = new List<LocationConnection>();
            isOnEdge = true;
        }
    }
}
