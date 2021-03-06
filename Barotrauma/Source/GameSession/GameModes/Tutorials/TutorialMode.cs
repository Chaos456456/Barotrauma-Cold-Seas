﻿using Microsoft.Xna.Framework.Graphics;
using Barotrauma.Tutorials;

namespace Barotrauma
{
    class TutorialMode : GameMode
    {
        public TutorialType tutorialType;
        
        public static void StartTutorial(TutorialType tutorialType)
        {
            Submarine.MainSub = Submarine.Load("Content/Map/TutorialSub.sub", "", true);
            
            tutorialType.Initialize();
        }

        public TutorialMode(GameModePreset preset, object param)
            : base(preset, param)
        {
        }

        public override void Start()
        {
            base.Start();

            tutorialType.Start();

        }

        public override void AddToGUIUpdateList()
        {
            tutorialType.AddToGUIUpdateList();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            tutorialType.Update(deltaTime);

        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            //CrewManager.Draw(spriteBatch);
            tutorialType.Draw(spriteBatch);
        }

    }
}
