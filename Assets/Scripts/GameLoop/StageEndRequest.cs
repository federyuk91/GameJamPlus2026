namespace GameJamPlus.Gameplay
{
    public readonly struct StageEndRequest
    {
        private readonly StageEndManager manager;
        private readonly long identifier;

        internal StageEndRequest(StageEndManager manager, long identifier)
        {
            this.manager = manager;
            this.identifier = identifier;
        }

        public void Complete()
        {
            if (manager != null) manager.CompleteAnimation(identifier);
        }
    }
}
