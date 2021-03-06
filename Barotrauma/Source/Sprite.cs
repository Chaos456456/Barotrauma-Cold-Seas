﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Linq;

namespace Barotrauma
{
    public class Sprite
    {
        static List<Sprite> list = new List<Sprite>();
        //the file from which the texture is loaded
        //if two sprites use the same file, they share the same texture
        string file;

        protected Texture2D texture;

        //the area in the texture that is supposed to be drawn
        Rectangle sourceRect;

        //the offset used when drawing the sprite
        protected Vector2 offset;

        protected Vector2 origin;

        //the size of the drawn sprite, if larger than the source,
        //the sprite is tiled to fill the target size
        public Vector2 size;

        public float rotation;

        public SpriteEffects effects;

        protected float depth;

        public Rectangle SourceRect
        {
            get { return sourceRect; }
            set { sourceRect = value; }
        }

        public float Depth
        {
            get { return depth; }
            set { depth = MathHelper.Clamp(value, 0.0f, 1.0f); }
        }

        public Vector2 Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        public Texture2D Texture
        {
            get { return texture; }
        }

        public string FilePath
        {
            get { return file; }
        }

        public override string ToString()
        {
            return FilePath + ": " + sourceRect;
        }

        public Sprite(XElement element, string path = "", string file = "")
        {
            if (file == "")
            {
                file = ToolBox.GetAttributeString(element, "texture", "");
            }
            
            if (file == "")
            {
                DebugConsole.ThrowError("Sprite " + element + " doesn't have a texture specified!");
                return;
            }

            if (!string.IsNullOrEmpty(path))
            {
                if (!path.EndsWith("/")) path += "/";
            }

            this.file = path + file;
            
            texture = LoadTexture(this.file);

            if (texture == null) return;
            
            Vector4 sourceVector = ToolBox.GetAttributeVector4(element, "sourcerect", Vector4.Zero);

            if (sourceVector.Z == 0.0f) sourceVector.Z = texture.Width;
            if (sourceVector.W == 0.0f) sourceVector.W = texture.Height;

            sourceRect = new Rectangle(
                (int)sourceVector.X, (int)sourceVector.Y,
                (int)sourceVector.Z, (int)sourceVector.W);

            origin = ToolBox.GetAttributeVector2(element, "origin", new Vector2(0.5f, 0.5f));
            origin.X = origin.X * sourceRect.Width;
            origin.Y = origin.Y * sourceRect.Height;

            size = ToolBox.GetAttributeVector2(element, "size", Vector2.One);
            size.X *= sourceRect.Width;
            size.Y *= sourceRect.Height;

            Depth = ToolBox.GetAttributeFloat(element, "depth", 0.0f);

            list.Add(this);
        }

        public Sprite(string newFile, Vector2 newOrigin)
        {
            file = newFile;
            texture = LoadTexture(file);

            if (texture == null) return;
            
            sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);

            size = new Vector2(sourceRect.Width, sourceRect.Height);

            origin = new Vector2((float)texture.Width * newOrigin.X, (float)texture.Height * newOrigin.Y);

            effects = SpriteEffects.None;

            list.Add(this);
        }

        public Sprite(Texture2D texture, Rectangle? sourceRectangle, Vector2? newOffset, float newRotation = 0.0f)
        {
            this.texture = texture;

            sourceRect = sourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);

            offset = newOffset ?? Vector2.Zero;

            size = new Vector2(sourceRect.Width, sourceRect.Height);

            origin = Vector2.Zero;

            effects = SpriteEffects.None;

            rotation = newRotation;

            list.Add(this);
        }

        public Sprite(string newFile, Rectangle? sourceRectangle, Vector2? newOffset, float newRotation = 0.0f)
        {
            file = newFile;
            texture = LoadTexture(file);

            sourceRect = sourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);
            
            offset = newOffset ?? Vector2.Zero; 
            
            size = new Vector2(sourceRect.Width, sourceRect.Height);

            origin = Vector2.Zero;
                        
            rotation = newRotation;

            list.Add(this);
        }
        
        public static Texture2D LoadTexture(string file)
        {
            foreach (Sprite s in list)
            {
                if (s.file == file) return s.texture;                
            }
            
            if (File.Exists(file))
            {
                return TextureLoader.FromFile(file);
            }
            else
            {
                DebugConsole.ThrowError("Sprite \""+file+"\" not found!");
            }

            return null;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, float rotate=0.0f, float scale=1.0f, SpriteEffects spriteEffect = SpriteEffects.None)
        {
            this.Draw(spriteBatch, pos, Color.White, rotate, scale, spriteEffect);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Color color, float rotate = 0.0f, float scale = 1.0f, SpriteEffects spriteEffect = SpriteEffects.None, float? depth = null)
        {
            this.Draw(spriteBatch, pos, color, this.origin, rotate, new Vector2(scale,scale), spriteEffect, depth);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos, Color color, Vector2 origin, float rotate = 0.0f, float scale = 1.0f, SpriteEffects spriteEffect = SpriteEffects.None, float? depth = null)
        {
            this.Draw(spriteBatch, pos, color, origin, rotate, new Vector2(scale, scale), spriteEffect, depth);
        }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 pos, Color color, Vector2 origin, float rotate, Vector2 scale, SpriteEffects spriteEffect = SpriteEffects.None, float? depth = null)
        {
            //for (int x = -1; x <= 1; x += 2)
            //{
            //    for (int y = -1; y <= 1; y += 2)
            //    {

            //        spriteBatch.Draw(texture, pos + offset + new Vector2(x, y) * 1.0f, sourceRect, Color.Black, rotation + rotate, origin, scale, spriteEffect, (depth == null ? this.depth : (float)depth) + 0.0001f);
            //    }
            //}

            if (texture == null) return;

            spriteBatch.Draw(texture, pos + offset, sourceRect, color, rotation + rotate, origin, scale, spriteEffect, depth == null ? this.depth : (float)depth);
        }
        
        public void DrawTiled(SpriteBatch spriteBatch, Vector2 pos, Vector2 targetSize, Color color)
        {
            DrawTiled(spriteBatch, pos, targetSize, Vector2.Zero, color);
        }
        
        public void DrawTiled(SpriteBatch spriteBatch, Vector2 pos, Vector2 targetSize, Color color, Point offset, float? overrideDepth = null)
        {
            //how many times the texture needs to be drawn on the x-axis
            int xTiles = (int)Math.Ceiling(targetSize.X / sourceRect.Width);
            //how many times the texture needs to be drawn on the y-axis
            int yTiles = (int)Math.Ceiling(targetSize.Y / sourceRect.Height);

            float depth = overrideDepth == null ? this.depth : (float)overrideDepth;

            Rectangle texPerspective = sourceRect;

            texPerspective.Location += offset;
            while (texPerspective.X >= sourceRect.Right)
                texPerspective.X = sourceRect.X + (texPerspective.X - sourceRect.Right);
            while (texPerspective.Y >= sourceRect.Bottom)
                texPerspective.Y = sourceRect.Y + (texPerspective.Y - sourceRect.Bottom);

            float top = pos.Y;
            texPerspective.Height = (int)Math.Min(targetSize.Y, sourceRect.Height);
            
            for (int y = 0; y < yTiles; y++)
            {
                var movementY = texPerspective.Height;
                texPerspective.Height = Math.Min((int)(targetSize.Y - texPerspective.Height * y), texPerspective.Height);
                
                float left = pos.X;
                texPerspective.Width = (int)Math.Min(targetSize.X, sourceRect.Width);

                for (int x = 0; x < xTiles; x++)
                {
                    var movementX = texPerspective.Width;
                    texPerspective.Width = Math.Min((int)(targetSize.X - texPerspective.Width * x), texPerspective.Width);

                    if (texPerspective.Right > sourceRect.Right)
                    {
                        int diff = texPerspective.Right - sourceRect.Right;
                        if (effects.HasFlag(SpriteEffects.FlipHorizontally))
                        {
                            spriteBatch.Draw(texture,
                                new Vector2(left, top),
                                new Rectangle(sourceRect.X, texPerspective.Y, diff, texPerspective.Height),
                                color, rotation, Vector2.Zero, 1.0f, effects, depth);

                            texPerspective.Width -= diff;
                            left += diff;
                        }
                        else
                        {
                            texPerspective.Width -= (int)diff;
                            spriteBatch.Draw(texture,
                                new Vector2(left + texPerspective.Width, top),
                                new Rectangle(sourceRect.X, texPerspective.Y, (int)diff, texPerspective.Height),
                                color, rotation, Vector2.Zero, 1.0f, effects, depth);
                        }
                    }
                    else if (texPerspective.Bottom > sourceRect.Bottom)
                    {
                        int diff = texPerspective.Bottom - sourceRect.Bottom;
                        texPerspective.Height -= diff;
                        spriteBatch.Draw(texture,
                            new Vector2(left, top + texPerspective.Height),
                            new Rectangle(texPerspective.X, sourceRect.Y, texPerspective.Width, diff),
                            color, rotation, Vector2.Zero, 1.0f, effects, depth);
                    }

                    spriteBatch.Draw(texture, new Vector2(left, top), texPerspective, color, rotation, Vector2.Zero, 1.0f, effects, depth);

                    if (texPerspective.X + movementX >= sourceRect.Right) texPerspective.X = sourceRect.X;
                    left += movementX;
                }

                if (texPerspective.Y + movementY >= sourceRect.Bottom) texPerspective.Y = sourceRect.Y;
                top += movementY;
            }
        }

        public void DrawTiled(SpriteBatch spriteBatch, Vector2 pos, Vector2 targetSize, Vector2 startOffset, Color color)
        {
            DrawTiled(spriteBatch, pos, targetSize, startOffset, sourceRect, color);
        }
        
        public void DrawTiled(SpriteBatch spriteBatch, Vector2 pos, Vector2 targetSize, Vector2 startOffset, Rectangle sourceRect, Color color)
        {
            //pos.X = (int)pos.X;
            //pos.Y = (int)pos.Y;

            //how many times the texture needs to be drawn on the x-axis
            int xTiles = (int)Math.Ceiling((targetSize.X+startOffset.X) / sourceRect.Width);
            //how many times the texture needs to be drawn on the y-axis
            int yTiles = (int)Math.Ceiling((targetSize.Y+startOffset.Y) / sourceRect.Height);

            Vector2 position = pos - startOffset;
            Rectangle drawRect = sourceRect;

            position.X = pos.X;

            for (int x = 0; x < xTiles; x++)
            {
                drawRect.X = sourceRect.X;
                drawRect.Height = sourceRect.Height;

                if (x == xTiles - 1)
                {
                    drawRect.Width -= (int)((position.X + sourceRect.Width) - (pos.X + targetSize.X));
                }
                else
                {
                    drawRect.Width = sourceRect.Width;
                }

                if (position.X < pos.X)
                {
                    float diff = pos.X - position.X;
                    position.X += diff;
                    drawRect.Width -= (int)diff;
                    drawRect.X += (int)diff;
                }

                position.Y = pos.Y;

                for (int y = 0; y < yTiles; y++)
                {
                    drawRect.Y = sourceRect.Y;

                    if (y == yTiles - 1)
                    {
                        drawRect.Height -= (int)((position.Y + sourceRect.Height) - (pos.Y + targetSize.Y));
                    }
                    else
                    {
                        drawRect.Height = sourceRect.Height;
                    }

                    if (position.Y < pos.Y)
                    {
                        int diff = (int)(pos.Y - position.Y);
                        position.Y += diff;
                        drawRect.Height -= diff;
                        drawRect.Y += diff;
                    }

                    spriteBatch.Draw(texture, position,
                        drawRect, color, rotation, Vector2.Zero, 1.0f, effects, depth);

                    position.Y += sourceRect.Height;
                }

                position.X += sourceRect.Width;
            }

        }

        public void Remove()
        {
            list.Remove(this);

            //check if another sprite is using the same texture
            foreach (Sprite s in list)
            {
                if (s.file == file) return;
            }

            //if not, free the texture
            if (texture != null)
            {
                texture.Dispose();
                texture = null;
            }
        }
        
    }

}

