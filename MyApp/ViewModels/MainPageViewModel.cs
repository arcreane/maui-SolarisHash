using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly IPlaceService _placeService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isLocationEnabled;

        [ObservableProperty]
        private string currentLocation = "Localisation en cours...";

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private string selectedFilter = "Tous";

        public ObservableCollection<Place> Places { get; } = new();
        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Tous", "Restaurants", "Monuments", "Musées", "Parcs", "Hôtels"
        };

        private Location _currentLocationCoords;

        public MainPageViewModel(IPlaceService placeService)
        {
            _placeService = placeService;
        }

        [RelayCommand]
        private async Task LoadPlacesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                
                // Obtenir la localisation
                await GetCurrentLocationAsync();
                
                if (_currentLocationCoords != null)
                {
                    // Récupérer les lieux proches
                    var places = await _placeService.GetNearbyPlacesAsync(
                        _currentLocationCoords.Latitude,
                        _currentLocationCoords.Longitude,
                        string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery
                    );

                    // Filtrer selon le filtre sélectionné
                    var filteredPlaces = ApplyFilter(places);

                    // Mettre à jour la collection
                    Places.Clear();
                    foreach (var place in filteredPlaces)
                    {
                        Places.Add(place);
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Erreur", 
                    $"Impossible de charger les lieux : {ex.Message}", 
                    "OK"
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchPlacesAsync()
        {
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task FilterChangedAsync()
        {
            if (Places.Any())
            {
                await LoadPlacesAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshLocationAsync()
        {
            await GetCurrentLocationAsync();
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task PlaceSelectedAsync(Place selectedPlace)
        {
            if (selectedPlace != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    selectedPlace.Name,
                    $"{selectedPlace.Description ?? "Aucune description disponible"}\n\n" +
                    $"📍 {selectedPlace.Address}\n" +
                    $"📏 Distance: {selectedPlace.FormattedDistance}\n" +
                    $"🏷️ Catégorie: {selectedPlace.MainCategory}",
                    "OK"
                );
            }
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    _currentLocationCoords = location;
                    CurrentLocation = $"📍 {location.Latitude:F4}, {location.Longitude:F4}";
                    IsLocationEnabled = true;
                }
                else
                {
                    CurrentLocation = "❌ Localisation indisponible";
                    IsLocationEnabled = false;
                }
            }
            catch (Exception ex)
            {
                CurrentLocation = $"❌ Erreur de localisation: {ex.Message}";
                IsLocationEnabled = false;
            }
        }

        private List<Place> ApplyFilter(List<Place> places)
        {
            if (SelectedFilter == "Tous" || string.IsNullOrEmpty(SelectedFilter))
                return places;

            return places.Where(p => 
                p.MainCategory.Contains(SelectedFilter, StringComparison.OrdinalIgnoreCase) ||
                p.Categories?.Any(c => c.Name.Contains(SelectedFilter, StringComparison.OrdinalIgnoreCase)) == true
            ).ToList();
        }

        partial void OnSearchQueryChanged(string value)
        {
            // Déclencher une recherche après un délai pour éviter trop d'appels
            Task.Run(async () =>
            {
                await Task.Delay(500); // Délai de 500ms
                if (SearchQuery == value) // Vérifier que la valeur n'a pas changé
                {
                    await SearchPlacesAsync();
                }
            });
        }
    }
}