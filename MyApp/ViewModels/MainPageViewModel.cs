using System.Collections.ObjectModel;
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
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isLocationEnabled;

        [ObservableProperty]
        private string currentLocation = "📍 Localisation en cours...";

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private string selectedFilter = "Tous";

        [ObservableProperty]
        private string statusMessage = "Prêt à chercher des lieux...";

        [ObservableProperty]
        private string selectedCityName = string.Empty;

        public ObservableCollection<Place> Places { get; } = new();
        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Tous", "Tourisme", "Restaurants", "Monuments", "Musées", "Parcs", "Services", "Commerce"
        };

        public ObservableCollection<string> PopularCities { get; } = new()
        {
            "Paris", "Lyon", "Marseille", "Toulouse", "Nice", "Nantes", "Montpellier", "Strasbourg", "Bordeaux", "Lille"
        };

        private Location? _currentLocationCoords;

        public MainPageViewModel(IPlaceService placeService, ILocationService locationService)
        {
            _placeService = placeService;
            _locationService = locationService;
            
            // S'abonner aux changements de localisation
            _locationService.LocationChanged += OnLocationChanged;
            
            Console.WriteLine($"🔍 Service utilisé: {_placeService.GetType().Name}");
            Console.WriteLine($"📍 Service de localisation: {_locationService.GetType().Name}");
        }

        [RelayCommand]
        private async Task LoadPlacesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = "🔍 Recherche en cours...";
                
                Console.WriteLine("🚀 Début de LoadPlacesAsync");
                
                // Obtenir la localisation intelligente
                await GetCurrentLocationAsync();
                
                if (_currentLocationCoords != null)
                {
                    Console.WriteLine($"📍 Position actuelle: {_currentLocationCoords.Latitude:F6}, {_currentLocationCoords.Longitude:F6}");
                    
                    StatusMessage = "🌐 Recherche de lieux...";
                    
                    var places = await _placeService.GetNearbyPlacesAsync(
                        _currentLocationCoords.Latitude,
                        _currentLocationCoords.Longitude,
                        string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                        radius: 2000,
                        limit: 50
                    );

                    Console.WriteLine($"🏠 Lieux trouvés avant filtrage: {places.Count}");
                    foreach (var place in places.Take(3))
                    {
                        Console.WriteLine($"   - {place.Name} ({place.Distance}m) - {place.MainCategory}");
                    }

                    var filteredPlaces = ApplyFilter(places);
                    Console.WriteLine($"🔽 Lieux après filtrage '{SelectedFilter}': {filteredPlaces.Count}");

                    Places.Clear();
                    foreach (var place in filteredPlaces.Take(20))
                    {
                        Places.Add(place);
                    }

                    StatusMessage = $"✅ {Places.Count} lieux trouvés";
                    Console.WriteLine($"✅ Places.Count final: {Places.Count}");
                }
                else
                {
                    StatusMessage = "❌ Impossible d'obtenir votre position";
                    Console.WriteLine("❌ _currentLocationCoords est null");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur: {ex.Message}";
                Console.WriteLine($"💥 Erreur dans LoadPlacesAsync: {ex.Message}");
                
                await Application.Current.MainPage.DisplayAlert(
                    "Erreur", 
                    $"Impossible de charger les lieux:\n{ex.Message}", 
                    "OK"
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoToCityAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCityName))
                return;

            try
            {
                IsLoading = true;
                StatusMessage = $"🏙️ Recherche de {SelectedCityName}...";
                
                Console.WriteLine($"🏙️ Recherche de la ville: {SelectedCityName}");
                
                var location = await _locationService.GetLocationByNameAsync(SelectedCityName);
                
                if (location != null)
                {
                    _currentLocationCoords = location;
                    CurrentLocation = $"🏙️ {SelectedCityName} ({location.Latitude:F4}, {location.Longitude:F4})";
                    IsLocationEnabled = true;
                    
                    // Rechercher les lieux dans cette ville
                    await LoadPlacesAsync();
                }
                else
                {
                    StatusMessage = $"❌ Ville '{SelectedCityName}' non trouvée";
                    await Application.Current.MainPage.DisplayAlert(
                        "Ville non trouvée", 
                        $"Impossible de trouver '{SelectedCityName}'.\nEssayez: Paris, Lyon, Marseille, etc.", 
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur recherche ville: {ex.Message}";
                Console.WriteLine($"❌ Erreur GoToCityAsync: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CitySelectedAsync(object selectedItem)
        {
            if (selectedItem is string cityName)
            {
                SelectedCityName = cityName;
                await GoToCityAsync();
            }
        }

        [RelayCommand]
        private async Task SearchPlacesAsync()
        {
            Console.WriteLine($"🔍 Recherche avec query: '{SearchQuery}'");
            StatusMessage = "🔍 Recherche avec filtre...";
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task FilterChangedAsync()
        {
            Console.WriteLine($"🔽 Filtre changé vers: '{SelectedFilter}'");
            if (Places.Any())
            {
                StatusMessage = $"🔽 Filtrage par: {SelectedFilter}";
                await LoadPlacesAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshLocationAsync()
        {
            Console.WriteLine("🔄 Actualisation de la position demandée");
            StatusMessage = "📍 Actualisation de la position...";
            _currentLocationCoords = null;
            await GetCurrentLocationAsync();
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task PlaceSelectedAsync(Place selectedPlace)
        {
            if (selectedPlace != null)
            {
                Console.WriteLine($"📍 Lieu sélectionné: {selectedPlace.Name}");
                
                var details = new List<string>();
                
                if (!string.IsNullOrEmpty(selectedPlace.Description))
                    details.Add($"📝 {selectedPlace.Description}");
                
                details.Add($"📍 {selectedPlace.Address}");
                details.Add($"📏 Distance: {selectedPlace.FormattedDistance}");
                details.Add($"🏷️ Catégorie: {selectedPlace.MainCategory}");
                
                if (!string.IsNullOrEmpty(selectedPlace.Phone))
                    details.Add($"📞 {selectedPlace.Phone}");
                
                if (!string.IsNullOrEmpty(selectedPlace.Website))
                    details.Add($"🌐 {selectedPlace.Website}");
                
                if (!string.IsNullOrEmpty(selectedPlace.OpeningHours))
                    details.Add($"🕐 {selectedPlace.OpeningHours}");

                await Application.Current.MainPage.DisplayAlert(
                    selectedPlace.Name,
                    string.Join("\n\n", details),
                    "OK"
                );
            }
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                Console.WriteLine("📍 Début de géolocalisation intelligente...");
                
                var location = await _locationService.GetCurrentLocationAsync();
                
                if (location != null)
                {
                    _currentLocationCoords = location;
                    CurrentLocation = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";
                    IsLocationEnabled = true;
                    
                    Console.WriteLine($"✅ Position obtenue: {location.Latitude:F6}, {location.Longitude:F6}");
                }
                else
                {
                    CurrentLocation = "❌ Localisation indisponible";
                    IsLocationEnabled = false;
                    Console.WriteLine("❌ Location est null");
                }
            }
            catch (Exception ex)
            {
                CurrentLocation = $"❌ Erreur: {ex.Message}";
                IsLocationEnabled = false;
                Console.WriteLine($"❌ Exception géolocalisation: {ex.Message}");
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _currentLocationCoords = e.NewLocation;
                CurrentLocation = $"📍 {e.NewLocation.Latitude:F6}, {e.NewLocation.Longitude:F6} ({e.Source})";
                IsLocationEnabled = true;
                
                Console.WriteLine($"📍 Localisation mise à jour: {e.NewLocation.Latitude:F6}, {e.NewLocation.Longitude:F6} via {e.Source}");
            });
        }

        private List<Place> ApplyFilter(List<Place> places)
        {
            if (SelectedFilter == "Tous" || string.IsNullOrEmpty(SelectedFilter))
                return places;

            var filtered = places.Where(p => 
            {
                var category = p.MainCategory.ToLower();
                var filter = SelectedFilter.ToLower();
                
                return filter switch
                {
                    "tourisme" => !string.IsNullOrEmpty(p.Tourism) || category.Contains("tourisme") || category.Contains("attraction"),
                    "restaurants" => !string.IsNullOrEmpty(p.Amenity) && (p.Amenity.Contains("restaurant") || p.Amenity.Contains("cafe") || p.Amenity.Contains("bar")),
                    "monuments" => !string.IsNullOrEmpty(p.Historic) || category.Contains("monument") || category.Contains("historique"),
                    "musées" => category.Contains("musée") || (!string.IsNullOrEmpty(p.Tourism) && p.Tourism.Contains("museum")),
                    "parcs" => category.Contains("parc") || category.Contains("jardin") || (!string.IsNullOrEmpty(p.Leisure) && (p.Leisure.Contains("park") || p.Leisure.Contains("garden"))),
                    "services" => !string.IsNullOrEmpty(p.Amenity) && !p.Amenity.Contains("restaurant") && !p.Amenity.Contains("cafe"),
                    "commerce" => !string.IsNullOrEmpty(p.Shop) || category.Contains("commerce"),
                    _ => category.Contains(filter)
                };
            }).ToList();
            
            Console.WriteLine($"🔽 Filtrage '{SelectedFilter}': {places.Count} -> {filtered.Count}");
            return filtered;
        }

        // Recherche avec délai pour éviter trop d'appels
        partial void OnSearchQueryChanged(string value)
        {
            Console.WriteLine($"🔤 SearchQuery changé: '{value}'");
            Task.Run(async () =>
            {
                await Task.Delay(800);
                if (SearchQuery == value && !IsLoading)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await SearchPlacesAsync());
                }
            });
        }

        // Nettoyer les événements
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

        // Méthode pour nettoyer les ressources
        public void Dispose()
        {
            if (_locationService != null)
            {
                _locationService.LocationChanged -= OnLocationChanged;
            }
        }
    }
}