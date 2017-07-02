﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Voronoi2;

namespace Barotrauma
{
    partial class Map
    {
        Vector2 difficultyIncrease = new Vector2(5.0f,10.0f);
        Vector2 difficultyCutoff = new Vector2(80.0f, 100.0f);

        private List<Level> levels;

        private List<Location> locations;

        private List<LocationConnection> connections;
        
        private string seed;
        private int size;

        private static Sprite iceTexture;
        private static Texture2D iceCraters;
        private static Texture2D iceCrack;

        private Location currentLocation;
        private Location selectedLocation;

        private Location highlightedLocation;

        private LocationConnection selectedConnection;

        public Location CurrentLocation
        {
            get { return currentLocation; }
        }

        public int CurrentLocationIndex
        {
            get { return locations.IndexOf(currentLocation); }
        }

        public Location SelectedLocation
        {
            get { return selectedLocation; }
        }

        public LocationConnection SelectedConnection
        {
            get { return selectedConnection; }
        }

        public string Seed
        {
            get { return seed; }
        }

        public static Map Load(XElement element)
        {
            string mapSeed = ToolBox.GetAttributeString(element, "seed", "a");

            int size = ToolBox.GetAttributeInt(element, "size", 500);
            Map map = new Map(mapSeed, size);

            map.SetLocation(ToolBox.GetAttributeInt(element, "currentlocation", 0));

            string discoveredStr = ToolBox.GetAttributeString(element, "discovered", "");

            string[] discoveredStrs = discoveredStr.Split(',');
            for (int i = 0; i < discoveredStrs.Length; i++ )
            {
                int index = -1;
                if (int.TryParse(discoveredStrs[i], out index)) map.locations[index].Discovered = true;
            }
            
            string passedStr = ToolBox.GetAttributeString(element, "passed", "");
            string[] passedStrs = passedStr.Split(',');
            for (int i = 0; i < passedStrs.Length; i++)
            {
                int index = -1;
                if (int.TryParse(passedStrs[i], out index)) map.connections[index].Passed = true;
            }
            
            return map;
        }

        public Map(string seed, int size)
        {
            this.seed = seed;

            this.size = size;

            levels = new List<Level>();

            locations = new List<Location>();

            connections = new List<LocationConnection>();

            if (iceTexture == null) iceTexture = new Sprite("Content/Map/iceSurface.png", Vector2.Zero);
            if (iceCraters == null) iceCraters = TextureLoader.FromFile("Content/Map/iceCraters.png");
            if (iceCrack == null)   iceCrack = TextureLoader.FromFile("Content/Map/iceCrack.png");
            
            Rand.SetSyncedSeed(ToolBox.StringToInt(this.seed));

            GenerateMap(CONNECTION.VORONOI);
            //GenerateLocations();

            /*currentLocation = locations[locations.Count / 2];
            currentLocation.Discovered = true;
            GenerateDifficulties(currentLocation, new List<LocationConnection> (connections), 10.0f);*/

            foreach (LocationConnection connection in connections)
            {
                connection.Level = Level.CreateRandom(connection);
            }
        }

        private void GenerateLocations()
        {
            Voronoi voronoi = new Voronoi(0.5f);

            List<Vector2> sites = new List<Vector2>();
            for (int i = 0; i < 50; i++)
            {
                sites.Add(new Vector2(Rand.Range(0.0f, size, false), Rand.Range(0.0f, size, false)));
            }
            
            List<GraphEdge> edges = voronoi.MakeVoronoiGraph(sites, size, size);
            
            sites.Clear();
            foreach (GraphEdge edge in edges)
            {
                if (edge.point1 == edge.point2) continue;

                //remove points from the edge of the map
                if (edge.point1.X == 0 || edge.point1.X == size) continue;
                if (edge.point1.Y == 0 || edge.point1.Y == size) continue;
                if (edge.point2.X == 0 || edge.point2.X == size) continue;
                if (edge.point2.Y == 0 || edge.point2.Y == size) continue;

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
                    locations.Add(newLocations[i]);
                }
                //int seed = (newLocations[0].GetHashCode() | newLocations[1].GetHashCode());
                connections.Add(new LocationConnection(newLocations[0], newLocations[1]));
            }

            float minDistance = 50.0f;
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                LocationConnection connection = connections[i];

                if (Vector2.Distance(connection.Locations[0].MapPosition, connection.Locations[1].MapPosition) > minDistance)
                {
                    continue;
                }

                locations.Remove(connection.Locations[0]);
                connections.Remove(connection);

                foreach (LocationConnection connection2 in connections)
                {
                    if (connection2.Locations[0] == connection.Locations[0]) connection2.Locations[0] = connection.Locations[1];
                    if (connection2.Locations[1] == connection.Locations[0]) connection2.Locations[1] = connection.Locations[1];
                }
            }

                foreach (LocationConnection connection in connections)
                {
                    connection.Locations[0].Connections.Add(connection);
                    connection.Locations[1].Connections.Add(connection);
                }

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                i = Math.Min(i, connections.Count - 1);

                LocationConnection connection = connections[i];

                for (int n = Math.Min(i - 1,connections.Count - 1); n >= 0; n--)
                {
                    if (connection.Locations.Contains(connections[n].Locations[0])
                        && connection.Locations.Contains(connections[n].Locations[1]))
                    {
                        connections.RemoveAt(n);                        
                    }
                }
            }

            foreach (LocationConnection connection in connections)
            {
                Vector2 start = connection.Locations[0].MapPosition;
                Vector2 end = connection.Locations[1].MapPosition;
                int generations = (int)(Math.Sqrt(Vector2.Distance(start, end) / 10.0f));
                connection.CrackSegments = MathUtils.GenerateJaggedLine(start, end, generations, 5.0f);
            }

        }

        private void GenerateDifficulties(Location start, List<LocationConnection> locations, float currDifficulty)
        {
            //start.Difficulty = currDifficulty;
            currDifficulty += Rand.Range(difficultyIncrease.X, difficultyIncrease.Y, false);
            if (currDifficulty > Rand.Range(difficultyCutoff.X, difficultyCutoff.Y, false)) currDifficulty = 10.0f;
            
            foreach (LocationConnection connection in start.Connections)
            {
                if (!locations.Contains(connection)) continue;

                Location nextLocation = connection.OtherLocation(start);
                locations.Remove(connection);

                connection.Difficulty = currDifficulty;
                
                GenerateDifficulties(nextLocation, locations, currDifficulty);
            }
        }

        public void MoveToNextLocation()
        {
            selectedConnection.Passed = true;

            currentLocation = selectedLocation;
            currentLocation.Discovered = true;
            selectedLocation = null;
        }

        public void SetLocation(int index)
        {
            if (index < 0 || index >= locations.Count)
            {
                DebugConsole.ThrowError("Location index out of bounds");
                return;
            }

            currentLocation = locations[index];
            currentLocation.Discovered = true;
        }

        public void Update(float deltaTime, Rectangle rect, float scale = 1.0f)
        {
            Vector2 rectCenter = new Vector2(rect.Center.X, rect.Center.Y);
            Vector2 offset = -currentLocation.MapPosition;

            float maxDist = 20.0f;
            float closestDist = 0.0f;
            highlightedLocation = null;
            for (int i = 0; i < locations.Count; i++)
            {
                Location location = locations[i];
                Vector2 pos = rectCenter + (location.MapPosition + offset) * scale;

                if (!rect.Contains(pos)) continue;

                float dist = Vector2.Distance(PlayerInput.MousePosition, pos);
                if (dist < maxDist && (highlightedLocation == null || dist < closestDist))
                {
                    closestDist = dist;
                    highlightedLocation = location;
                }
            }

            foreach (LocationConnection connection in connections)
            {
                if (highlightedLocation != currentLocation &&
                    connection.Locations.Contains(highlightedLocation) && connection.Locations.Contains(currentLocation))
                {
                    if (PlayerInput.LeftButtonClicked() &&
                        selectedLocation != highlightedLocation && highlightedLocation != null)
                    {
                        selectedConnection = connection;
                        selectedLocation = highlightedLocation;
                        GameMain.LobbyScreen.SelectLocation(highlightedLocation, connection);
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle rect, float scale = 1.0f)
        {
            Vector2 rectCenter = new Vector2(rect.Center.X, rect.Center.Y);
            Vector2 offset = -currentLocation.MapPosition;

            List<Color> colors = new List<Color>{ Color.Red, Color.Green, Color.Blue, Color.Pink, Color.Orange, Color.Yellow, Color.Cyan, Color.Magenta, Color.Black, Color.White };

            iceTexture.DrawTiled(spriteBatch, new Vector2(rect.X, rect.Y), new Vector2(rect.Width, rect.Height), Vector2.Zero, Color.White*0.8f);
            
            foreach (GraphEdge edge in graphEdges)
            {
                if (edge.node1 != null && edge.node2 != null)
                {
                    Color color;

                    float dist = Vector2.Distance(edge.node1.mapPosition, edge.node2.mapPosition);

                    int width = (int)(MathHelper.Clamp(connections[0].Difficulty, 2.0f, 20.0f) * scale);

                    if (edge.isInternal)
                        color = Color.Red;
                    else
                        color = Color.Blue;
                    
                    spriteBatch.Draw(iceCrack,
                        new Rectangle((int)edge.node1.mapPosition.X, (int)edge.node1.mapPosition.Y, (int)dist + 2, width),
                        new Rectangle(0, 0, iceCrack.Width, 60), color, MathUtils.VectorToAngle(edge.node2.mapPosition - edge.node1.mapPosition),
                        new Vector2(0, 30), SpriteEffects.None, 0.01f);
                }
            }

            foreach (VoronoiCell cell in graphCells)
            {
                float dist = Vector2.Distance(cell.Center, cell.Center);

                int width = (int)(MathHelper.Clamp(connections[0].Difficulty, 2.0f, 20.0f) * 20.0f);
                
                if (cell.group != -1)
                spriteBatch.Draw(iceCrack,
                    new Rectangle((int)cell.Center.X, (int)cell.Center.Y, (int)dist + 2, width),
                    new Rectangle(0, 0, iceCrack.Width, 60), colors[cell.group %( colors.Count - 1)], MathUtils.VectorToAngle(cell.Center - cell.Center),
                    new Vector2(0, 30), SpriteEffects.None, 0.01f);
            }




            foreach (LocationConnection connection in connections)
            {
                Color crackColor = Color.White * Math.Max(connection.Difficulty/100.0f, 1.5f);

                if (selectedLocation != currentLocation &&
                    (connection.Locations.Contains(selectedLocation) && connection.Locations.Contains(currentLocation)))
                {
                    crackColor = Color.Red;
                }
                else if (highlightedLocation != currentLocation &&
                (connection.Locations.Contains(highlightedLocation) && connection.Locations.Contains(currentLocation)))
                {
                    crackColor = Color.Red * 0.5f;
                }
                else if (!connection.Passed)
                {
                    crackColor *= 0.2f;
                }
                
                for (int i = 0; i < connection.CrackSegments.Count; i++ )
                {
                    var segment = connection.CrackSegments[i];

                    Vector2 start   = rectCenter + (segment[0] + offset) * scale;
                    Vector2 end     = rectCenter + (segment[1] + offset) * scale;

                    /*if (!rect.Contains(start) && !rect.Contains(end))
                    {
                        continue;
                    }
                    else*/
                    {
                        Vector2? intersection = MathUtils.GetLineRectangleIntersection(start, end, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, rect.Height));
                        if (intersection != null)
                        {
                            if (!rect.Contains(start))
                            {
                                start = (Vector2)intersection;
                            }
                            else
                            {
                                end = (Vector2)intersection;
                            }
                        }
                    }

                    float dist = Vector2.Distance(start, end);

                    int width = (int)(MathHelper.Clamp(connection.Difficulty, 2.0f, 20.0f) * scale);

                    spriteBatch.Draw(iceCrack,
                        new Rectangle((int)start.X, (int)start.Y, (int)dist + 2, width),
                        new Rectangle(0, 0, iceCrack.Width, 60), crackColor, MathUtils.VectorToAngle(end - start),
                        new Vector2(0, 30), SpriteEffects.None, 0.01f);
                }
            }

            rect.Inflate(8, 8);
            GUI.DrawRectangle(spriteBatch, rect, Color.Black, false, 0.0f, 8);
            GUI.DrawRectangle(spriteBatch, rect, Color.LightGray);

            for (int i = 0; i < locations.Count; i++)
            {
                Location location = locations[i];
                Vector2 pos = rectCenter + (location.MapPosition + offset) * scale;
                
                Rectangle drawRect = location.Type.Sprite.SourceRect;
                Rectangle sourceRect = drawRect;
                drawRect.X = (int)pos.X - drawRect.Width/2;
                drawRect.Y = (int)pos.Y - drawRect.Width/2;

                if (!rect.Intersects(drawRect)) continue;

                Color color = location.Connections.Find(c => c.Locations.Contains(currentLocation))==null ? Color.White : Color.Green;

                color *= (location.Discovered) ? 0.8f : 0.2f;

                if (location == currentLocation) color = Color.Orange;

                if (drawRect.X < rect.X)
                {
                    sourceRect.X += rect.X - drawRect.X;
                    sourceRect.Width -= sourceRect.X;
                    drawRect.X = rect.X;
                }
                else if (drawRect.Right > rect.Right)
                {
                    sourceRect.Width -= (drawRect.Right - rect.Right);
                }

                if (drawRect.Y < rect.Y)
                {
                    sourceRect.Y += rect.Y - drawRect.Y;
                    sourceRect.Height -= sourceRect.Y;
                    drawRect.Y = rect.Y;
                }
                else if (drawRect.Bottom > rect.Bottom)
                {
                    sourceRect.Height -= drawRect.Bottom - rect.Bottom;
                }

                drawRect.Width = sourceRect.Width;
                drawRect.Height = sourceRect.Height;
                
                spriteBatch.Draw(location.Type.Sprite.Texture, drawRect, sourceRect, color);
            }

            for (int i = 0; i < 3; i++ )
            {
                Location location = (i == 0) ? highlightedLocation : selectedLocation;
                if (i == 2) location = currentLocation;
                
                if (location == null) continue;

                Vector2 pos = rectCenter + (location.MapPosition + offset) * scale;
                pos.X = (int)(pos.X + location.Type.Sprite.SourceRect.Width*0.6f);
                pos.Y = (int)(pos.Y - 10);
                GUI.DrawString(spriteBatch, pos, location.Name, Color.White, Color.Black * 0.8f, 3);
            }

        }

        public void Save(XElement element)
        {
            XElement mapElement = new XElement("map");

            mapElement.Add(new XAttribute("currentlocation", CurrentLocationIndex));
            mapElement.Add(new XAttribute("seed", Seed));
            mapElement.Add(new XAttribute("size", size));

            List<int> discoveredLocations = new List<int>();
            for (int i = 0; i < locations.Count; i++ )
            {
                if (locations[i].Discovered) discoveredLocations.Add(i);
            }
            mapElement.Add(new XAttribute("discovered", string.Join(",", discoveredLocations)));

            List<int> passedConnections = new List<int>();
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].Passed) passedConnections.Add(i);
            }
            mapElement.Add(new XAttribute("passed", string.Join(",", passedConnections)));

            element.Add(mapElement);
        }
    }


    partial class LocationConnection
    {
        private Location[] locations;
        private Level level;

        public float Difficulty;

        public List<Vector2[]> CrackSegments;

        public bool Passed;

        private int missionsCompleted;

        private Mission mission;
        public Mission Mission
        {
            get 
            {
                if (mission == null || mission.Completed)
                {
                    if (mission != null && mission.Completed) missionsCompleted++;

                    long seed = (long)locations[0].MapPosition.X + (long)locations[0].MapPosition.Y * 100;
                    seed += (long)locations[1].MapPosition.X * 10000 + (long)locations[1].MapPosition.Y * 1000000;

                    MTRandom rand = new MTRandom((int)((seed + missionsCompleted) % int.MaxValue));

                    if (rand.NextDouble() < 0.3f) return null;

                    mission = Mission.LoadRandom(locations, rand, "", true);
                }

                return mission;
            }
        }

        public Location[] Locations
        {
            get { return locations; }
        }
        
        public Level Level
        {
            get { return level; }
            set { level = value; }
        }
        
        public LocationConnection(Location location1, Location location2)
        {
            locations = new Location[] { location1, location2 };

            missionsCompleted = 0;
        }

        public Location OtherLocation(Location location)
        {
            if (locations[0] == location)
            {
                return locations[1];
            }
            else if (locations[1] == location)
            {
                return locations[0];
            }
            else
            {
                return null;
            }
        }
    }
}
