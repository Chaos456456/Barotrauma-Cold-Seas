﻿using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Barotrauma
{
    class GameMode
    {
        public static List<GameModePreset> PresetList = new List<GameModePreset>();

        protected DateTime startTime;

        //public readonly bool IsSinglePlayer;

        protected bool isRunning;

        //protected string name;

        protected GameModePreset preset;

        private string endMessage;

        public virtual Mission Mission
        {
            get { return null; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public bool IsSinglePlayer
        {
            get { return preset.IsSinglePlayer; }
        }

        public string Name
        {
            get { return preset.Name; }
        }

        public string EndMessage
        {
            get { return endMessage; }
        }

        public GameModePreset Preset
        {
            get { return preset; }
        }

        public GameMode(GameModePreset preset, object param)
        {
            this.preset = preset;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }

        public virtual void Start()
        {
            startTime = DateTime.Now;
            //if (duration!=TimeSpan.Zero)
            //{
            //    endTime = startTime + duration;
            //    this.duration = duration;

            //    timerBar = new GUIProgressBar(new Rectangle(GameMain.GraphicsWidth - 120, 20, 100, 25), Color.Gold, 0.0f, null);  
            //}

            endMessage = "The round has ended!";

            isRunning = true;
        }

        public virtual void MsgBox() { }

        public virtual void AddToGUIUpdateList() { }

        public virtual void Update(float deltaTime)
        {
            //if (!isRunning) return;

            //if (duration != TimeSpan.Zero)
            //{
            //    double elapsedTime = (DateTime.Now - startTime).TotalSeconds;
            //    timerBar.BarSize = (float)(elapsedTime / duration.TotalSeconds);
            //}
            //if (DateTime.Now >= endTime)
            //{
            //    End(endMessage);                
            //}
        }

        public virtual void End(string endMessage = "")
        {
            isRunning = false;

            if (endMessage != "" || this.endMessage == null) this.endMessage = endMessage;

            GameMain.GameSession.EndShift(endMessage);
        }
        

    }
}
