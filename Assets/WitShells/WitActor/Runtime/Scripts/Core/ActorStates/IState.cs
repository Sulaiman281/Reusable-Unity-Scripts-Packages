namespace WitShells.WitActor
{
    public interface IState
    {
        /// <summary>
        /// Called when the state is entered.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called when the state is exited.
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called every frame while the state is active.
        /// </summary>
        void OnUpdate();
    }
}