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
        private readonly IOrientationService? _orientationService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isLocationEnabled;

        [ObservableProperty]
        private bool isOrientationFilterEnabled;

        [ObservableProperty]
        private string currentLocation = "📍 Localisation en cours...";

        [ObservableProperty]
        private string currentOrientation = "📱 Orientation inconnue";

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
        private List<Place> _allPlaces = new();

        public MainPageViewModel(IPlaceService placeService, ILocationService locationService, IOrientationService orientationService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 MainPageViewModel: Initialisation...");
                
                _placeService = placeService ?? throw new ArgumentNullException(nameof(placeService));
                _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
                _orientationService = orientationService; // Peut être null
                
                // S'abonner aux changements
                if (_locationService != null)
                {
                    _locationService.LocationChanged += OnLocationChanged;
                    System.Diagnostics.Debug.WriteLine("✅ LocationService connecté");
                }
                
                if (_orientationService != null)
                {
                    _orientationService.OrientationChanged += OnOrientationChanged;
                    System.Diagnostics.Debug.WriteLine("✅ OrientationService connecté");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ OrientationService non disponible");
                }
                
                System.Diagnostics.Debug.WriteLine($"🔍 Service utilisé: {_placeService.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"📍 Service de localisation: {_locationService.GetType().Name}");
                
                StatusMessage = "✅ TravelBuddy initialisé avec succès !";
                System.Diagnostics.Debug.WriteLine("✅ MainPageViewModel: Initialisation terminée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MainPageViewModel: Erreur initialisation - {ex.Message}");
                StatusMessage = $"❌ Erreur d'initialisation: {ex.Message}";
                throw;
            }
        }

        [RelayCommand]
        private async Task LoadPlacesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = "🔍 Recherche en cours...";
                
                System.Diagnostics.Debug.WriteLine("🚀 Début de LoadPlacesAsync");
                
                // Vérifier d'abord si on a déjà une position définie
                if (_currentLocationCoords == null)
                {
                    await GetCurrentLocationAsync();
                }
                
                if (_currentLocationCoords != null)
                {
                    System.Diagnostics.Debug.WriteLine($"📍 Position utilisée pour recherche: {_currentLocationCoords.Latitude:F6}, {_currentLocationCoords.Longitude:F6}");
                    
                    StatusMessage = "🌐 Recherche de lieux réels...";
                    
                    var places = await _placeService.GetNearbyPlacesAsync(
                        _currentLocationCoords.Latitude,
                        _currentLocationCoords.Longitude,
                        string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                        radius: 3000, // 3km
                        limit: 100    // 100 lieux max
                    );

                    System.Diagnostics.Debug.WriteLine($"🏠 Lieux trouvés depuis API: {places.Count}");
                    
                    // Stocker tous les lieux
                    _allPlaces = places;

                    // Appliquer les filtres
                    var filteredPlaces = ApplyAllFilters(_allPlaces);

                    Places.Clear();
                    
                    foreach (var place in filteredPlaces.Take(50))
                    {
                        Places.Add(place);
                    }

                    if (Places.Any())
                    {
                        StatusMessage = $"✅ {Places.Count} lieux trouvés près de {GetLocationName()}";
                        System.Diagnostics.Debug.WriteLine($"✅ Places.Count final: {Places.Count}");
                    }
                    else
                    {
                        StatusMessage = $"⚠️ Aucun lieu trouvé près de {GetLocationName()} (rayon 3km)";
                        System.Diagnostics.Debug.WriteLine("⚠️ Aucun lieu après filtrage");
                    }
                }
                else
                {
                    StatusMessage = "❌ Impossible d'obtenir votre position";
                    System.Diagnostics.Debug.WriteLine("❌ _currentLocationCoords est null");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"💥 Erreur dans LoadPlacesAsync: {ex.Message}");
                
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Erreur", 
                        $"Impossible de charger les lieux:\n{ex.Message}", 
                        "OK"
                    );
                }
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
                
                System.Diagnostics.Debug.WriteLine($"🏙️ Recherche ville: {SelectedCityName}");
                
                var location = await _locationService.GetLocationByNameAsync(SelectedCityName);
                
                if (location != null)
                {
                    _currentLocationCoords = location;
                    CurrentLocation = $"🏙️ {SelectedCityName} ({location.Latitude:F4}, {location.Longitude:F4})";
                    IsLocationEnabled = true;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Coordonnées trouvées pour {SelectedCityName}: {location.Latitude:F6}, {location.Longitude:F6}");
                    
                    // Vider les anciens résultats
                    Places.Clear();
                    _allPlaces.Clear();
                    
                    StatusMessage = $"📍 Position mise à jour pour {SelectedCityName}";
                }
                else
                {
                    StatusMessage = $"❌ Ville '{SelectedCityName}' non trouvée";
                    System.Diagnostics.Debug.WriteLine($"❌ Ville non trouvée: {SelectedCityName}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur recherche ville: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GoToCityAsync: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"🔍 Recherche avec query: '{SearchQuery}'");
            StatusMessage = "🔍 Recherche avec filtre...";
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task FilterChangedAsync()
        {
            System.Diagnostics.Debug.WriteLine($"🔽 Filtre changé vers: '{SelectedFilter}'");
            if (_allPlaces.Any())
            {
                StatusMessage = $"🔽 Filtrage par: {SelectedFilter}";
                await RefreshPlacesAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshLocationAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Actualisation de la position demandée");
            StatusMessage = "📍 Actualisation de la position...";
            
            // Réinitialiser complètement la position
            _currentLocationCoords = null;
            Places.Clear();
            _allPlaces.Clear();
            SelectedCityName = string.Empty;
            
            await GetCurrentLocationAsync();
        }

        [RelayCommand]
        private async Task ToggleOrientationFilterAsync()
        {
            if (_orientationService == null)
            {
                StatusMessage = "❌ Service d'orientation non disponible";
                return;
            }

            try
            {
                IsOrientationFilterEnabled = !IsOrientationFilterEnabled;
                
                if (IsOrientationFilterEnabled)
                {
                    await _orientationService.StartAsync();
                    StatusMessage = "🧭 Filtrage par orientation activé - Pointez votre téléphone !";
                    System.Diagnostics.Debug.WriteLine("🧭 Service d'orientation démarré");
                }
                else
                {
                    await _orientationService.StopAsync();
                    StatusMessage = "🧭 Filtrage par orientation désactivé";
                    System.Diagnostics.Debug.WriteLine("🛑 Service d'orientation arrêté");
                }
                
                // Reappliquer les filtres
                await RefreshPlacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur orientation: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ToggleOrientationFilter: {ex.Message}");
                
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Erreur capteurs", 
                        $"Impossible d'activer l'orientation:\n{ex.Message}", 
                        "OK"
                    );
                }
            }
        }

        [RelayCommand]
        private async Task PlaceSelectedAsync(Place selectedPlace)
        {
            if (selectedPlace != null && Application.Current?.MainPage != null)
            {
                System.Diagnostics.Debug.WriteLine($"📍 Lieu sélectionné: {selectedPlace.Name}");
                
                var details = new List<string>
                {
                    $"📍 {selectedPlace.Address}",
                    $"📏 Distance: {selectedPlace.FormattedDistance}",
                    $"🏷️ Catégorie: {selectedPlace.MainCategory}"
                };
                
                if (!string.IsNullOrEmpty(selectedPlace.Description))
                    details.Insert(0, $"📝 {selectedPlace.Description}");

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
                System.Diagnostics.Debug.WriteLine("📍 Début de géolocalisation...");

                var location = await _locationService.GetCurrentLocationAsync();

                if (location != null)
                {
                    _currentLocationCoords = location;
                    CurrentLocation = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";
                    IsLocationEnabled = true;

                    System.Diagnostics.Debug.WriteLine($"✅ Position obtenue: {location.Latitude:F6}, {location.Longitude:F6}");
                }
                else
                {
                    CurrentLocation = "❌ Localisation indisponible";
                    IsLocationEnabled = false;
                    System.Diagnostics.Debug.WriteLine("❌ Location est null");
                }
            }
            catch (Exception ex)
            {
                CurrentLocation = $"❌ Erreur: {ex.Message}";
                IsLocationEnabled = false;
                System.Diagnostics.Debug.WriteLine($"❌ Exception géolocalisation: {ex.Message}");
            }
        }

        private async Task RefreshPlacesAsync()
        {
            if (_allPlaces.Any() && _currentLocationCoords != null)
            {
                var filteredPlaces = ApplyAllFilters(_allPlaces);
                
                Places.Clear();
                foreach (var place in filteredPlaces.Take(50))
                {
                    Places.Add(place);
                }
                
                StatusMessage = $"✅ {Places.Count} lieux trouvés près de {GetLocationName()}";
            }
            await Task.CompletedTask;
        }

        private List<Place> ApplyAllFilters(List<Place> places)
        {
            var filtered = places;
            
            // Filtre par catégorie
            filtered = ApplyFilter(filtered);
            
            // Filtre par orientation si activé
            if (IsOrientationFilterEnabled && _currentLocationCoords != null && _orientationService != null)
            {
                filtered = _orientationService.FilterPlacesByOrientation(
                    filtered, 
                    _currentLocationCoords.Latitude, 
                    _currentLocationCoords.Longitude,
                    tolerance: 60.0
                );
            }
            
            return filtered.OrderBy(p => p.Distance).ToList();
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _currentLocationCoords = e.NewLocation;
                CurrentLocation = $"📍 {e.NewLocation.Latitude:F6}, {e.NewLocation.Longitude:F6} ({e.Source})";
                IsLocationEnabled = true;
                
                System.Diagnostics.Debug.WriteLine($"📍 Localisation mise à jour: {e.NewLocation.Latitude:F6}, {e.NewLocation.Longitude:F6} via {e.Source}");
            });
        }

        private void OnOrientationChanged(object? sender, OrientationChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                CurrentOrientation = $"📱 {e.DirectionName} ({e.Heading:F0}°)";
                
                if (IsOrientationFilterEnabled && _allPlaces.Any() && _currentLocationCoords != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🧭 Orientation changée: {e.Heading:F0}° - Mise à jour des lieux");
                    await RefreshPlacesAsync();
                }
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
            
            return filtered;
        }

        private string GetLocationName()
        {
            if (!string.IsNullOrEmpty(SelectedCityName))
                return SelectedCityName;
            
            if (_currentLocationCoords != null)
                return "coordonnées actuelles";
            
            return "position inconnue";
        }

        public void Dispose()
        {
            try
            {
                if (_locationService != null)
                {
                    _locationService.LocationChanged -= OnLocationChanged;
                }

                if (_orientationService != null)
                {
                    _orientationService.OrientationChanged -= OnOrientationChanged;
                    Task.Run(async () => await _orientationService.StopAsync());
                }

                System.Diagnostics.Debug.WriteLine("✅ MainPageViewModel: Ressources nettoyées");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur dispose: {ex.Message}");
            }
        }
    }
}