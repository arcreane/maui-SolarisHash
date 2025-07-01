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

        public ObservableCollection<Place> Places { get; } = new();
        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Tous", "Tourisme", "Restaurants", "Monuments", "Musées", "Parcs", "Services", "Commerce"
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
                StatusMessage = "🔍 Recherche en cours...";
                
                // Obtenir la localisation
                await GetCurrentLocationAsync();
                
                if (_currentLocationCoords != null)
                {
                    StatusMessage = "🌐 Interrogation d'OpenStreetMap...";
                    
                    // Récupérer les lieux proches via Overpass API
                    var places = await _placeService.GetNearbyPlacesAsync(
                        _currentLocationCoords.Latitude,
                        _currentLocationCoords.Longitude,
                        string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                        radius: 1000, // 1km
                        limit: 50
                    );

                    // Filtrer selon le filtre sélectionné
                    var filteredPlaces = ApplyFilter(places);

                    // Mettre à jour la collection
                    Places.Clear();
                    foreach (var place in filteredPlaces.Take(20)) // Limiter à 20 pour l'affichage
                    {
                        Places.Add(place);
                    }

                    StatusMessage = $"✅ {Places.Count} lieux trouvés";
                }
                else
                {
                    StatusMessage = "❌ Impossible d'obtenir votre position";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur: {ex.Message}";
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
        private async Task SearchPlacesAsync()
        {
            StatusMessage = "🔍 Recherche avec filtre...";
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task FilterChangedAsync()
        {
            if (Places.Any())
            {
                StatusMessage = $"🔽 Filtrage par: {SelectedFilter}";
                await LoadPlacesAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshLocationAsync()
        {
            StatusMessage = "📍 Actualisation de la position...";
            await GetCurrentLocationAsync();
            await LoadPlacesAsync();
        }

        [RelayCommand]
        private async Task PlaceSelectedAsync(Place selectedPlace)
        {
            if (selectedPlace != null)
            {
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
                // Vérifier les permissions
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    CurrentLocation = "❌ Permission de localisation refusée";
                    IsLocationEnabled = false;
                    return;
                }

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
            catch (FeatureNotSupportedException)
            {
                CurrentLocation = "❌ Géolocalisation non supportée";
                IsLocationEnabled = false;
            }
            catch (FeatureNotEnabledException)
            {
                CurrentLocation = "❌ Géolocalisation désactivée";
                IsLocationEnabled = false;
            }
            catch (PermissionException)
            {
                CurrentLocation = "❌ Permission de géolocalisation refusée";
                IsLocationEnabled = false;
            }
            catch (Exception ex)
            {
                CurrentLocation = $"❌ Erreur: {ex.Message}";
                IsLocationEnabled = false;
            }
        }

        private List<Place> ApplyFilter(List<Place> places)
        {
            if (SelectedFilter == "Tous" || string.IsNullOrEmpty(SelectedFilter))
                return places;

            return places.Where(p => 
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
        }

        // Recherche avec délai pour éviter trop d'appels
        partial void OnSearchQueryChanged(string value)
        {
            Task.Run(async () =>
            {
                await Task.Delay(800); // Délai de 800ms
                if (SearchQuery == value && !IsLoading) // Vérifier que la valeur n'a pas changé
                {
                    MainThread.BeginInvokeOnMainThread(async () => await SearchPlacesAsync());
                }
            });
        }
    }
}