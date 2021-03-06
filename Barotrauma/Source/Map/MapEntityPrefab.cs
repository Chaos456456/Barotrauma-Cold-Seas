﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Barotrauma
{
    [Flags]
    enum MapEntityCategory
    {
        Structure = 1, Machine = 2, Equipment = 4, Electrical = 8, Material = 16, Misc = 32, Alien = 64
    }

    class MapEntityPrefab
    {
        public static List<MapEntityPrefab> list = new List<MapEntityPrefab>();

        protected string name;

        public List<string> tags;

        protected bool isLinkable;

        public Sprite sprite;

        //the position where the structure is being placed (needed when stretching the structure)
        protected static Vector2 placePosition;

        protected ConstructorInfo constructor;

        //is it possible to stretch the entity horizontally/vertically
        public bool resizeHorizontal { get; protected set; }
        public bool resizeVertical { get; protected set; }

        //which prefab has been selected for placing
        protected static MapEntityPrefab selected;

        protected int price;

        public string Name
        {
            get { return name; }
        }

        public static MapEntityPrefab Selected
        {
            get { return selected; }
            set { selected = value; }
        }


        public string Description
        {
            get;
            protected set;
        }

        public virtual bool IsLinkable
        {
            get { return isLinkable; }
        }

        public bool ResizeHorizontal
        {
            get { return resizeHorizontal; }
        }

        public bool ResizeVertical
        {
            get { return resizeVertical; }
        }

        public MapEntityCategory Category
        {
            get;
            protected set;
        }

        public Color SpriteColor
        {
            get;
            protected set;
        }

        public int Price
        {
            get { return price; }
        }

        public static void Init()
        {
            MapEntityPrefab ep = new MapEntityPrefab();
            ep.name = "Hull";
            ep.Description = "Hulls determine which parts are considered to be \"inside the sub\". Generally every room should be enclosed by a hull.";
            ep.constructor = typeof(Hull).GetConstructor(new Type[] { typeof(MapEntityPrefab), typeof(Rectangle) });
            ep.resizeHorizontal = true;
            ep.resizeVertical = true;
            list.Add(ep);

            ep = new MapEntityPrefab();
            ep.name = "Gap";
            ep.Description = "Gaps allow water and air to flow between two hulls. ";
            ep.constructor = typeof(Gap).GetConstructor(new Type[] { typeof(MapEntityPrefab), typeof(Rectangle) });
            ep.resizeHorizontal = true;
            ep.resizeVertical = true;
            list.Add(ep);

            ep = new MapEntityPrefab();
            ep.name = "Waypoint";
            ep.constructor = typeof(WayPoint).GetConstructor(new Type[] { typeof(MapEntityPrefab), typeof(Rectangle) });
            list.Add(ep);

            ep = new MapEntityPrefab();
            ep.name = "Spawnpoint";
            ep.constructor = typeof(WayPoint).GetConstructor(new Type[] { typeof(MapEntityPrefab), typeof(Rectangle) });
            list.Add(ep);
            
            //ep = new MapEntityPrefab();
            //ep.name = "Linked Submarine";
            //ep.Category = 0;
            //list.Add(ep);

        }

        public MapEntityPrefab()
        {
            Category = MapEntityCategory.Structure;
        }

        public virtual void UpdatePlacing(Camera cam)
        {
            Vector2 placeSize = Submarine.GridSize;

            if (placePosition == Vector2.Zero)
            {
                Vector2 position = Submarine.MouseToWorldGrid(cam, Submarine.MainSub);
                
                if (PlayerInput.LeftButtonHeld()) placePosition = position;
            }
            else
            {
                Vector2 position = Submarine.MouseToWorldGrid(cam, Submarine.MainSub);

                if (resizeHorizontal) placeSize.X = position.X - placePosition.X;
                if (resizeVertical) placeSize.Y = placePosition.Y - position.Y;
                
                Rectangle newRect = Submarine.AbsRect(placePosition, placeSize);
                newRect.Width = (int)Math.Max(newRect.Width, Submarine.GridSize.X);
                newRect.Height = (int)Math.Max(newRect.Height, Submarine.GridSize.Y);

                if (Submarine.MainSub != null)
                {
                    newRect.Location -= Submarine.MainSub.Position.ToPoint();
                }

                if (PlayerInput.LeftButtonReleased())
                {
                    CreateInstance(newRect);
                    placePosition = Vector2.Zero;
                    selected = null;
                }

                newRect.Y = -newRect.Y;
            }

            if (PlayerInput.RightButtonHeld())
            {
                placePosition = Vector2.Zero;
                selected = null;
            }
        }

        public virtual void DrawPlacing(SpriteBatch spriteBatch, Camera cam)
        {
            Vector2 placeSize = Submarine.GridSize;

            if (placePosition == Vector2.Zero)
            {
                Vector2 position = Submarine.MouseToWorldGrid(cam, Submarine.MainSub);

                GUI.DrawLine(spriteBatch, new Vector2(position.X - GameMain.GraphicsWidth, -position.Y), new Vector2(position.X + GameMain.GraphicsWidth, -position.Y), Color.White,0,(int)(2.0f/cam.Zoom));

                GUI.DrawLine(spriteBatch, new Vector2(position.X, -(position.Y - GameMain.GraphicsHeight)), new Vector2(position.X, -(position.Y + GameMain.GraphicsHeight)), Color.White, 0, (int)(2.0f / cam.Zoom));
            }
            else
            {
                Vector2 position = Submarine.MouseToWorldGrid(cam, Submarine.MainSub);

                if (resizeHorizontal) placeSize.X = position.X - placePosition.X;
                if (resizeVertical) placeSize.Y = placePosition.Y - position.Y;

                Rectangle newRect = Submarine.AbsRect(placePosition, placeSize);
                newRect.Width = (int)Math.Max(newRect.Width, Submarine.GridSize.X);
                newRect.Height = (int)Math.Max(newRect.Height, Submarine.GridSize.Y);

                if (Submarine.MainSub != null)
                {
                    newRect.Location -= Submarine.MainSub.Position.ToPoint();
                }
                
                newRect.Y = -newRect.Y;
                GUI.DrawRectangle(spriteBatch, newRect, Color.DarkBlue);
            }
        }

        protected virtual void CreateInstance(Rectangle rect)
        {
            object[] lobject = new object[] { this, rect };
            constructor.Invoke(lobject);
        }    

        public static bool SelectPrefab(object selection)
        {
            if ((selected = selection as MapEntityPrefab) != null)
            {
                placePosition = Vector2.Zero;
                return true;
            }
            else
            {
                return false;
            }
        }

        //a method that allows the GUIListBoxes to check through a delegate if the entityprefab is still selected
        public static object GetSelected()
        {
            return (object)selected;            
        }
        
        public void DrawListLine(SpriteBatch spriteBatch, Vector2 pos, Color color)
        {            
            GUI.Font.DrawString(spriteBatch, name, pos, color);
        }

    }
}
