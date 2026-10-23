// Datos que viajan entre la escena del Overworld y la escena del nivel.
public static class OverworldSession
{
    // true cuando el nivel se cargó desde el Overworld: el menú principal se saltea
    // y se va directo a la preselección de barco.
    public static bool SkipMainMenu;

    // Nombre del océano elegido (por si el nivel lo quiere mostrar).
    public static string CurrentOcean;

    public static bool ConsumeSkipMenu()
    {
        bool value = SkipMainMenu;
        SkipMainMenu = false;
        return value;
    }
}
