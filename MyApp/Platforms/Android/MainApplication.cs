using Android.App;
using Android.Runtime;

namespace MyApp
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // ✅ AJOUTÉ: Initialisation Maps pour Android
        public override void OnCreate()
        {
            base.OnCreate();
            
            // Initialiser les cartes avec une clé API (optionnel pour le mode de base)
            
#if DEBUG
            Console.WriteLine("📱 Application Android initialisée avec Maps");
#endif
        }
    }
}