using Services;

public class ServicesManager : MonoSingleton<ServicesManager>
{
    public void Initialize()
    {
        InitializeSingleton(true);
    }
}
