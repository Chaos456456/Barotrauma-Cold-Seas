using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Voronoi2;

// COLD SEAS
//
// Partial Map class, implementing most the functions used by Cold Seas
//

namespace Barotrauma
{
    partial class Map
    {
        Voronoi voronoiGraph;
        List<GraphEdge> graphEdges;
        List<VoronoiCell> graphCells;
        List<VoronoiCell>[,] graphCellsGrid;
        List<List<VoronoiCell>> groups;
        List<PrimaryNode> primaryNodes;
        List<SecondaryNode> secondaryNodes;

        float minDistance = 50.0f;
        int gapChance = 6;

        enum CONNECTION
        {
            NEAREST_NEIGHBOR = 0,
            HELD_KARP = 1,
            TREE = 2,
            VORONOI = 3
        }

        private void GenerateMap(CONNECTION Algorithm)
        {
            GenerateVoronoi();

            CreateCellGroups();

            GeneratePrimaryNodes();

            CleanInternalEdges();

            GenerateSecondaryNodes();

            GenerateConnections();

            GenerateJaggedLines();

            /*foreach (GraphEdge edge in graphEdges)
            {
                if (edge.point1 == edge.point2) continue;

                Location[] newLocations = new Location[2];
                newLocations[0] = locations.Find(l => l.MapPosition == edge.point1 || l.MapPosition == edge.point2);
                newLocations[1] = locations.Find(l => l != newLocations[0] && (l.MapPosition == edge.point1 || l.MapPosition == edge.point2));

                for (int i = 0; i < 2; i++)
                {
                    if (newLocations[i] != null) continue;

                    Vector2[] points = new Vector2[] { edge.point1, edge.point2 };

                    int positionIndex = Rand.Int(1, false);

                    Vector2 position = points[positionIndex];
                    if (newLocations[1 - i] != null && newLocations[1 - i].MapPosition == position) position = points[1 - positionIndex];

                    newLocations[i] = Location.CreateRandom(position);

                    // DEBUG
                    newLocations[i].Discovered = true;

                    locations.Add(newLocations[i]);
                }
                ConnectLocations(newLocations[0], newLocations[1]);
            }

            for (int i = locations.Count - 1; i >= 0; i--)
            {
                Location closeNode = null;

                if (isTooCloseToExistingLocations(locations[i], out closeNode))
                {
                    foreach (LocationConnection connection in locations[i].Connections)
                    {
                        if (connection.Locations[0] == locations[i])
                            connection.Locations[0] = closeNode;
                        if (connection.Locations[1] == locations[i])
                            connection.Locations[1] = closeNode;
                    }
                    locations.RemoveAt(i);
                }
            }

            /*foreach (GraphEdge edge in graphEdges)
            {
                if (!edge.isInternal && !(edge.node1 == null || edge.node2 == null))
                {
                    Location newLocation1;
                    Location newLocation2;

                    newLocation1 = Location.CreateRandom(edge.node1.mapPosition);
                    newLocation2 = Location.CreateRandom(edge.node2.mapPosition);

                    if (!locations.Any(l => l.MapPosition == edge.node1.mapPosition))
                        locations.Add(newLocation1);
                    else
                        newLocation2 = locations.Find(l => l.MapPosition == edge.node1.mapPosition);
                    if (!locations.Any(l => l.MapPosition == edge.node2.mapPosition))
                        locations.Add(newLocation2);
                    else
                        newLocation2 = locations.Find(l => l.MapPosition == edge.node2.mapPosition);

                    ConnectLocations(newLocation1, newLocation2);
                }
            }*/

            //GenerateConnections(Algorithm);

            currentLocation = locations[locations.Count / 2];
            currentLocation.Discovered = true;
            GenerateDifficulties(currentLocation, new List<LocationConnection>(connections), 10.0f);
        }

        private void GenerateVoronoi()
        {
            voronoiGraph = new Voronoi(minDistance);

            List<Vector2> sites = new List<Vector2>();
            for (int i = 0; i < 50; i++)
            {
                bool tooClose = false;
                Vector2 newSite = new Vector2(Rand.Range(0.0f, size, false), Rand.Range(0.0f, size, false));

                foreach (Vector2 site in sites)
                {
                    if (Vector2.Distance(newSite, site) <= minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose && !(newSite.X == 0 || newSite.X == size || newSite.Y == 0 || newSite.Y == size))
                    sites.Add(newSite);
                else
                    i--;
            }

            graphEdges = voronoiGraph.MakeVoronoiGraph(sites, size, size);

            graphCells = CaveGenerator.GraphEdgesToCells(graphEdges, new Rectangle(0, 0, size, size), (float)size, out graphCellsGrid);

            sites.Clear();
        }

        private void CreateCellGroups()
        {
            //graphCells = CaveGenerator.GraphEdgesToCells(graphEdges, new Rectangle(0, 0, size, size), (float)size, out graphCellsGrid);
            List<VoronoiCell> unassignedCells = new List<VoronoiCell>();
            groups = new List<List<VoronoiCell>>();
            int currentGroup = 0;

            foreach (VoronoiCell cell in graphCells)
                if (cell.cellState == VoronoiCell.CELLSTATE.UNASSIGNED)
                    unassignedCells.Add(cell);

            while (unassignedCells.Count > 0)
            {
                int cellIndex = Rand.Range(0, unassignedCells.Count - 1, false);
                int cellNumber = Rand.Range(1, 6, false);
                List<VoronoiCell> groupCells = new List<VoronoiCell>();
                List<VoronoiCell> validGroupCells = new List<VoronoiCell>();
                VoronoiCell firstCell = unassignedCells[cellIndex];

                firstCell.group = currentGroup;
                firstCell.cellState = VoronoiCell.CELLSTATE.ASSIGNED;
                groupCells.Add(firstCell);
                validGroupCells.Add(firstCell);
                unassignedCells.Remove(firstCell);

                while (validGroupCells.Count > 0 && validGroupCells.Count < cellNumber)
                {
                    int groupCellIndex = Rand.Range(0, validGroupCells.Count - 1, false);
                    VoronoiCell selectedCell = validGroupCells[groupCellIndex];
                    List<GraphEdge> validEdges = selectedCell.getValidEdgesGroup();

                    if (validEdges.Count == 0)
                    {
                        validGroupCells.Remove(selectedCell);
                        continue;
                    }

                    int edgeIndex = Rand.Range(0, validEdges.Count - 1, false);
                    GraphEdge selectedEdge = validEdges[edgeIndex];
                    VoronoiCell adjacentCell = selectedCell.getAdjacentCellFromEdge(selectedEdge);

                    adjacentCell.group = currentGroup;
                    adjacentCell.cellState = VoronoiCell.CELLSTATE.ASSIGNED;
                    groupCells.Add(adjacentCell);
                    validGroupCells.Add(adjacentCell);
                    unassignedCells.Remove(adjacentCell);
                }

                foreach (VoronoiCell cell in groupCells)
                {
                    //List<GraphEdge> validEdges = cell.getValidEdges();

                    foreach (GraphEdge edge in cell.edges)
                    {
                        VoronoiCell adjacentCell = cell.getAdjacentCellFromEdge(edge);
                        if (adjacentCell != null && adjacentCell.cellState == VoronoiCell.CELLSTATE.UNASSIGNED)
                        {
                            adjacentCell.cellState = VoronoiCell.CELLSTATE.BLANK;
                            adjacentCell.group = -1;
                            unassignedCells.Remove(adjacentCell);
                        }
                    }
                }
                groups.Add(groupCells);
                currentGroup++;
            }
            foreach (GraphEdge edge in graphEdges)
            {
                if ((edge.cell1 != null && edge.cell2 != null) && edge.cell1.group == edge.cell2.group)
                    edge.isInternal = true;
            }
        }

        private void GeneratePrimaryNodes()
        {
            primaryNodes = new List<PrimaryNode>();

            foreach (GraphEdge edge in graphEdges)
            {
                PrimaryNode node1 = new PrimaryNode(edge.point1);
                PrimaryNode node2 = new PrimaryNode(edge.point2);

                if (edge.point1 == edge.point2 || edge.cell1 == null || edge.cell2 == null) continue;


                if (primaryNodes.Any(n => n.mapPosition == node1.mapPosition))
                    node1 = primaryNodes.Find(n => n.mapPosition == node1.mapPosition);
                else
                    primaryNodes.Add(node1);
                if (primaryNodes.Any(n => n.mapPosition == node2.mapPosition))
                    node2 = primaryNodes.Find(n => n.mapPosition == node2.mapPosition);
                else
                    primaryNodes.Add(node2);

                edge.node1 = node1;
                edge.node2 = node2;

                node1.AddEdge(edge);
            }



            /*foreach (PrimaryNode node in primaryNodes)
            {
                List<int> adjacentGroups = new List<int>();

                foreach (VoronoiCell cell in node.adjacentCells)
                {
                    if (cell == null)
                    {
                        adjacentGroups = new List<int>(1);
                        break;
                    }
                    if (!adjacentGroups.Contains(cell.group))
                        adjacentGroups.Add(cell.group);
                }
                if (adjacentGroups.Count == 1 && adjacentGroups[0] != -1)
                    node.invalid = true;
            }*/
        }

        private void CleanInternalEdges()
        {
            int i = 0;

            foreach (GraphEdge edge in graphEdges)
            {
                if (edge.cell1 == null || edge.cell2 == null)
                    continue;
                if (edge.cell1.group == edge.cell2.group)
                {
                    edge.isInternal = true;
                    i++;
                }
            }

            /*for (int i = graphEdges.Count - 1; i >= 0; i--)
            {
                GraphEdge edge = graphEdges[i];
                if (edge.isInternal)
                    graphEdges.Remove(edge);
            }*/
        }

        private void GenerateSecondaryNodes()
        {
            secondaryNodes = new List<SecondaryNode>();

            foreach (VoronoiCell cell in graphCells)
            {
                List<GraphEdge> validEdges = cell.edges.Where(e => e.isInternal == false).ToList();

                if (validEdges.Count > 0)
                {
                    SecondaryNode centerNode = new SecondaryNode(cell.Center, cell);

                    for (int i = 0; i < 2 && validEdges.Count > 0; i++)
                    {
                        int edgeIndex = Rand.Range(0, validEdges.Count - 1, false);
                        GraphEdge selectedEdge = validEdges[edgeIndex];
                        SecondaryNode newNode;

                        validEdges.Remove(selectedEdge);
                        newNode = selectedEdge.AddSecondaryNode();

                        if (newNode == null)
                            continue;

                        secondaryNodes.Add(newNode);
                        centerNode.attachedNodes.Add(newNode);
                    }

                    if (centerNode.attachedNodes.Count > 0)
                        secondaryNodes.Add(centerNode);
                }


                /*if (cell.edges.Any(e => e.isInternal == false))
                {
                    SecondaryNode node = new SecondaryNode(cell.Center, cell);
                    List<SecondaryNode> secondNodes = new List<SecondaryNode>();

                    if (cell.edges.Count(e => e.isInternal == false) > 1)
                    {
                        List<GraphEdge> edges = new List<GraphEdge>(cell.edges.Where(e => e.isInternal == false));

                        int firstEdgeIndex = Rand.Range(0, edges.Count - 1, false);
                        int secondEdgeIndex = Rand.Range(0, edges.Count - 2, false);

                        secondaryNodes.Add(new SecondaryNode(cell.Center, edges[firstEdgeIndex]));
                        edges.RemoveAt(firstEdgeIndex);
                        secondaryNodes.Add(new SecondaryNode(cell.Center, edges[secondEdgeIndex]));
                    }
                    else
                    {
                        secondNodes.Add(new SecondaryNode(cell.Center, cell.edges.First(e => e.isInternal == false)));

                    }
                }*/
            }
        }

        private void GenerateConnections()
        {
            foreach (GraphEdge edge in graphEdges)
            {
                if (edge.point1 == edge.point2 || edge.isInternal) continue;

                Location[] newLocations = new Location[2];
                newLocations[0] = locations.Find(l => l.MapPosition == edge.point1 || l.MapPosition == edge.point2);
                newLocations[1] = locations.Find(l => l != newLocations[0] && (l.MapPosition == edge.point1 || l.MapPosition == edge.point2));

                for (int i = 0; i < 2; i++)
                {
                    if (newLocations[i] != null) continue;

                    Vector2[] points = new Vector2[] { edge.point1, edge.point2 };

                    int positionIndex = Rand.Int(1, false);

                    Vector2 position = points[positionIndex];
                    if (newLocations[1 - i] != null && newLocations[1 - i].MapPosition == position) position = points[1 - positionIndex];

                    newLocations[i] = Location.CreateRandom(position);

                    // DEBUG
                    //newLocations[i].Discovered = true;

                    locations.Add(newLocations[i]);
                }
                LocationConnection newConnection = ConnectLocations(newLocations[0], newLocations[1]);
                newConnection.primaryNodes[0] = edge.node1;
                newConnection.primaryNodes[1] = edge.node2;
                newConnection.secondaryNodes = edge.secondaryNodes.ToArray();

                /*if (!edge.isInternal && edge.node1 != null && edge.node2 != null && edge.cell1 != null && edge.cell2 != null)
                {
                    Location newLocation1;
                    Location newLocation2;

                    newLocation1 = Location.CreateRandom(edge.node1.mapPosition);
                    newLocation2 = Location.CreateRandom(edge.node2.mapPosition);

                    if (!locations.Any(l => l.MapPosition == edge.node1.mapPosition))
                        locations.Add(newLocation1);
                    else
                        newLocation2 = locations.Find(l => l.MapPosition == edge.node1.mapPosition);
                    if (!locations.Any(l => l.MapPosition == edge.node2.mapPosition))
                        locations.Add(newLocation2);
                    else
                        newLocation2 = locations.Find(l => l.MapPosition == edge.node2.mapPosition);

                    ConnectLocations(newLocation1, newLocation2);
            }*/
            }
            foreach (SecondaryNode node in secondaryNodes)
            {
                if (!node.isOnEdge)
                {
                    Location nodeLocation = Location.CreateRandom(node.mapPosition);
                    locations.Add(nodeLocation);
                    foreach (SecondaryNode subNode in node.attachedNodes)
                    {
                        LocationConnection newConnection = ConnectLocations(nodeLocation, Location.CreateRandom(subNode.mapPosition));
                        subNode.connections.Add(newConnection);
                    }
                }
            }
        }

        private void GenerateJaggedLines()
        {
            foreach (LocationConnection connection in connections)
            {
                Vector2 start = connection.Locations[0].MapPosition;
                Vector2 end = connection.Locations[1].MapPosition;
                int generations = (int)(Math.Sqrt(Vector2.Distance(start, end) / 10.0f));
                //connection.CrackSegments = MathUtils.GenerateJaggedLine(start, end, generations, 5.0f);
                connection.CrackSegments = MathUtils.GenerateJaggedLine(start, end, generations, 0.0f);
            }
        }



























        /*private void GenerateLocationsNoConnect()
        {
            foreach (GraphEdge graphEdges in graphEdges)
            {
                if (graphEdges.point1 == graphEdges.point2) continue;

                //remove points from the graphEdges of the map
                if (graphEdges.point1.X == 0 || graphEdges.point1.X == size) continue;
                if (graphEdges.point1.Y == 0 || graphEdges.point1.Y == size) continue;
                if (graphEdges.point2.X == 0 || graphEdges.point2.X == size) continue;
                if (graphEdges.point2.Y == 0 || graphEdges.point2.Y == size) continue;

                Location[] newLocations = new Location[2];
                newLocations[0] = locations.Find(l => l.MapPosition == graphEdges.point1 || l.MapPosition == graphEdges.point2);
                newLocations[1] = locations.Find(l => l != newLocations[0] && (l.MapPosition == graphEdges.point1 || l.MapPosition == graphEdges.point2));

                for (int i = 0; i < 2; i++)
                {
                    if (newLocations[i] != null) continue;

                    Vector2[] points = new Vector2[] { graphEdges.point1, graphEdges.point2 };

                    int positionIndex = Rand.Int(1, false);

                    Vector2 position = points[positionIndex];
                    if (newLocations[1 - i] != null && newLocations[1 - i].MapPosition == position) position = points[1 - positionIndex];

                    newLocations[i] = Location.CreateRandom(position);

                    // DEBUG
                    newLocations[i].Discovered = true;

                    //if (!isTooCloseToExistingLocations(newLocations[i]))
                        locations.Add(newLocations[i]);
                }
            }
        }

        private void GenerateConnections(CONNECTION Algorithm)
        {
            GenerateConnectionsVoronoi();

            foreach (LocationConnection connection in connections)
            {
                Vector2 start = connection.Locations[0].MapPosition;
                Vector2 end = connection.Locations[1].MapPosition;
                int generations = (int)(Math.Sqrt(Vector2.Distance(start, end) / 10.0f));
                //connection.CrackSegments = MathUtils.GenerateJaggedLine(start, end, generations, 5.0f);
                connection.CrackSegments = MathUtils.GenerateJaggedLine(start, end, generations, 0.0f);
            }
        }

        private void GenerateConnectionsVoronoi()
        {
            graphCells = CaveGenerator.GraphEdgesToCells(graphEdges, new Rectangle(0, 0, size, size), (float)size, out graphCellsGrid);

            CreateCellGroups();

            GenerateLocationsVoronoi();
        }

        private void CreateCellGroups()
        {
            graphCells = CaveGenerator.GraphEdgesToCells(graphEdges, new Rectangle(0, 0, size, size), (float)size, out graphCellsGrid);
            List<VoronoiCell> unassignedCells = new List<VoronoiCell>();
            groups = new List<List<VoronoiCell>>();
            int currentGroup = 0;

            foreach (VoronoiCell cell in graphCells)
                if (cell.cellState == VoronoiCell.CELLSTATE.UNASSIGNED)
                    unassignedCells.Add(cell);

            while (unassignedCells.Count > 0)
            {
                int cellIndex = Rand.Range(0, unassignedCells.Count - 1, false);
                int cellNumber = Rand.Range(1, 6, false);
                List<VoronoiCell> groupCells = new List<VoronoiCell>();
                VoronoiCell firstCell = unassignedCells[cellIndex];

                firstCell.group = currentGroup;
                firstCell.cellState = VoronoiCell.CELLSTATE.ASSIGNED;
                groupCells.Add(firstCell);
                unassignedCells.Remove(firstCell);

                while (groupCells.Count < cellNumber)
                {
                    int groupCellIndex = Rand.Range(0, groupCells.Count - 1, false);
                    VoronoiCell selectedCell = groupCells[groupCellIndex];
                    List<GraphEdge> validEdges = selectedCell.getValidEdges();
                    int edgeIndex = Rand.Range(0, validEdges.Count - 1, false);
                    GraphEdge selectedEdge = selectedCell.edges[edgeIndex];
                    VoronoiCell adjacentCell = selectedCell.getAdjacentCellFromEdge(selectedEdge);

                    adjacentCell.group = currentGroup;
                    adjacentCell.cellState = VoronoiCell.CELLSTATE.ASSIGNED;
                    groupCells.Add(adjacentCell);
                    unassignedCells.Remove(adjacentCell);
                }

                foreach (VoronoiCell cell in groupCells)
                {
                    List<GraphEdge> validEdges = cell.getValidEdges();

                    foreach (GraphEdge edge in validEdges)
                    {
                        VoronoiCell adjacentCell = cell.getAdjacentCellFromEdge(edge);
                        if (adjacentCell.cellState == VoronoiCell.CELLSTATE.UNASSIGNED)
                        {
                            adjacentCell.cellState = VoronoiCell.CELLSTATE.BLANK;
                            unassignedCells.Remove(adjacentCell);
                        }
                    }
                }
                groups.Add(groupCells);
                currentGroup++;
            }
            for (int i = graphEdges.Count - 1; i >= 0; i--)
            {
                GraphEdge edge = graphEdges[i];
                if (edge.cell1 == null || edge.cell2 == null || edge.cell1.cellState == edge.cell2.cellState)
                    graphEdges.Remove(edge);
            }
        }

        private void GenerateLocationsVoronoi()
        {
            primaryNodes = new List<PrimaryNode>();

            foreach (GraphEdge graphEdge in graphEdges)
            {
                if (graphEdge.point1 == graphEdge.point2) continue;

                //remove points from the graphEdges of the map
                if (graphEdge.point1.X == 0 || graphEdge.point1.X == size) continue;
                if (graphEdge.point1.Y == 0 || graphEdge.point1.Y == size) continue;
                if (graphEdge.point2.X == 0 || graphEdge.point2.X == size) continue;
                if (graphEdge.point2.Y == 0 || graphEdge.point2.Y == size) continue;

                PrimaryNode[] newNodes = new PrimaryNode[2];
                newNodes[0] = primaryNodes.Find(l => l.mapPosition == graphEdge.point1 || l.mapPosition == graphEdge.point2);
                newNodes[1] = primaryNodes.Find(l => l != newNodes[0] && (l.mapPosition == graphEdge.point1 || l.mapPosition == graphEdge.point2));

                for (int i = 0; i < 2; i++)
                {
                    if (newNodes[i] != null)
                    {
                        newNodes[i].AddEdge(graphEdge);
                        continue;
                    }

                    Vector2[] points = new Vector2[] { graphEdge.point1, graphEdge.point2 };

                    int positionIndex = Rand.Int(1, false);

                    Vector2 position = points[positionIndex];
                    if (newNodes[1 - i] != null && newNodes[1 - i].mapPosition == position) position = points[1 - positionIndex];

                    newNodes[i] = new PrimaryNode(position);
                    newNodes[i].AddEdge(graphEdge);

                    // DEBUG
                    //newNodes[i].Discovered = true;

                    PrimaryNode closeNode;

                    if (!isTooCloseToExistingNode(newNodes[i], out closeNode) && newNodes[i].adjacentCells.Any(c => c.cellState == VoronoiCell.CELLSTATE.ASSIGNED))
                        primaryNodes.Add(newNodes[i]);
                    else
                    {
                        if (newNodes[i].adjacentCells.Any(c => c.cellState == VoronoiCell.CELLSTATE.ASSIGNED))
                        {
                            closeNode.AddEdge(graphEdge, newNodes[i]);
                            closeNode.aggregate = true;
                        }
                    }
                    if (graphEdge.cell1.cellState == VoronoiCell.CELLSTATE.ASSIGNED && (graphEdge.cell1.group == graphEdge.cell2.group))
                        graphEdge.isInternal = true;
                }
            }

            foreach (List<VoronoiCell> group in groups)
            {
                int selectedCell = Rand.Range(0, group.Count - 1, false);

                group[selectedCell].hasGap = (Rand.Range(0, 10, false) < gapChance);

                foreach (GraphEdge edge in group[selectedCell].edges)
                {
                    if (edge.isInternal)
                    {
                        edge.isInternal = false;
                        break;
                    }
                }
            }

            foreach (List<VoronoiCell> group in groups)
            {
                foreach (VoronoiCell cell in group)
                {
                    foreach (GraphEdge edge in cell.edges)
                    {
                        if (!edge.isInternal && !(edge.node1 == null || edge.node2 == null))
                        {
                            Location newLocation1;
                            Location newLocation2;

                            newLocation1 = Location.CreateRandom(edge.node1.mapPosition);
                            newLocation2 = Location.CreateRandom(edge.node2.mapPosition);

                            if (!locations.Any(l => l.MapPosition == edge.node1.mapPosition))
                                locations.Add(newLocation1);
                            else
                                newLocation2 = locations.Find(l => l.MapPosition == edge.node1.mapPosition);
                            if (!locations.Any(l => l.MapPosition == edge.node2.mapPosition))
                                locations.Add(newLocation2);
                            else
                                newLocation2 = locations.Find(l => l.MapPosition == edge.node2.mapPosition);

                            ConnectLocations(newLocation1, newLocation2);
                        }
                    }
                }
            }
        }*/

        private bool isTooCloseToExistingLocations(Location newLocation, out Location outLocation)
        {
            bool tooClose = false;
            outLocation = null;

            foreach (Location location in locations)
            {
                if (location != newLocation)
                {
                    tooClose = (Vector2.Distance(location.MapPosition, newLocation.MapPosition) <= minDistance) == true ? true : tooClose;
                    if (tooClose)
                    {
                        outLocation = location;
                        break;
                    }
                }
            }

            return (tooClose);
        }

        private bool isTooCloseToExistingNode(PrimaryNode newNode, out PrimaryNode outNode)
        {
            bool tooClose = false;
            outNode = null;

            foreach (PrimaryNode node in primaryNodes)
            {
                tooClose = (Vector2.Distance(node.mapPosition, newNode.mapPosition) <= minDistance) == true ? true : tooClose;
                outNode = node;
            }

            return (tooClose);
        }

        //DEBUG
        /*foreach (GraphEdge edge in graphEdges)
        {
            Location newLocation1;
            Location newLocation2;

            if ((edge.node1 != null && edge.node2 != null) && (edge.node1.group == edge.node2.group))
            {
                if (primaryNodes.Any(n => n.mapPosition == edge.node1.mapPosition) && primaryNodes.Any(n => n.mapPosition == edge.node2.mapPosition))
                {
                    if (!locations.Any(l => l.MapPosition == edge.node1.mapPosition))
                    {
                        newLocation1 = Location.CreateRandom(edge.node1.mapPosition);
                        locations.Add(newLocation1);
                    }
                    else
                        newLocation1 = locations.Find(l => l.MapPosition == edge.node1.mapPosition);
                    if (!locations.Any(l => l.MapPosition == edge.node2.mapPosition))
                    {
                        newLocation2 = Location.CreateRandom(edge.node2.mapPosition);
                        locations.Add(newLocation2);
                    }
                    else
                        newLocation2 = locations.Find(l => l.MapPosition == edge.node2.mapPosition);

                    //connections.Add(new LocationConnection(newLocation1, newLocation2));
                    ConnectLocations(newLocation1, newLocation2);
                }
            }
        }*/

        private LocationConnection ConnectLocations(Location location1, Location location2)
        {
            LocationConnection newConnection = new LocationConnection(location1, location2);

            connections.Add(newConnection); 
            location1.Connections.Add(newConnection);
            location2.Connections.Add(newConnection);

            newConnection.primaryNodes = new PrimaryNode[2];

            return (newConnection);
        }

        private void ConnectPrimaryNodes(PrimaryNode node1, PrimaryNode node2)
        {
            LocationConnection newConnection = new LocationConnection(node1, node2);

            connections.Add(newConnection);
            node1.connections.Add(newConnection);
            node2.connections.Add(newConnection);
        }

        private void ConnectSecondaryNodes(SecondaryNode node1, SecondaryNode node2)
        {
            LocationConnection newConnection = new LocationConnection(node1, node2);

            connections.Add(newConnection);
            node1.connections.Add(newConnection);
            node2.connections.Add(newConnection);
        }
    }

    class GraphNode
    {
        public float distance = 0f;
        public short location;
        public List<GraphNode> nextNodes;

        public GraphNode(short newLocation)
        {
            location = newLocation;
            nextNodes = new List<GraphNode>();
        }
    }
}
