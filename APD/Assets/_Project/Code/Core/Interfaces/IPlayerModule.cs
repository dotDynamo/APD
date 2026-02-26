namespace _Project.Code.Core.Interfaces
{
    public interface IPlayerModule
    {
        void ModuleEnable();
        void ModuleDisable();
        void Tick(float dt);
    }
}