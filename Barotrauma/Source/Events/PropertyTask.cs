﻿namespace Barotrauma
{
    class PropertyTask : Task
    {
        Item item;

        
        public delegate bool IsFinishedHandler();
        private IsFinishedHandler IsFinishedChecker;

        public PropertyTask(Item item, IsFinishedHandler isFinished, float priority, string name)
            : base(priority, name)
        {
            if (taskManager == null) return;

            this.item = item;
            IsFinishedChecker = isFinished;
        }

        public override void Update(float deltaTime)
        {
            if (IsFinishedChecker())
            {
                Finished();
            }
        }
    }
}
