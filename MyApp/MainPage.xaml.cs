using MyApp.ViewModels;

namespace MyApp
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        private async void OnFilterChanged(object sender, EventArgs e)
        {
            if (_viewModel.FilterChangedCommand.CanExecute(null))
            {
                await _viewModel.FilterChangedCommand.ExecuteAsync(null);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Nettoyer les ressources quand la page disparaît
            _viewModel?.Dispose();
        }
    }
}